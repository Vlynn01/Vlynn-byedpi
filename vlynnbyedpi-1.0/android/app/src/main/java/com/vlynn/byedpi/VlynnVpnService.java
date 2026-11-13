package com.vlynn.byedpi;

import android.app.Notification;
import android.app.NotificationChannel;
import android.app.NotificationManager;
import android.app.PendingIntent;
import android.content.Context;
import android.content.Intent;
import android.content.SharedPreferences;
import android.content.pm.ServiceInfo;
import android.net.VpnService;
import android.os.Build;
import android.os.ParcelFileDescriptor;

import java.io.ByteArrayOutputStream;
import java.io.FileInputStream;
import java.io.FileOutputStream;
import java.io.InputStream;
import java.io.OutputStream;
import java.net.DatagramPacket;
import java.net.DatagramSocket;
import java.net.HttpURLConnection;
import java.net.InetAddress;
import java.net.URL;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;
import java.util.concurrent.atomic.AtomicBoolean;
import java.util.concurrent.atomic.AtomicLong;

public class VlynnVpnService extends VpnService {

    public static final String ACTION_START = "com.vlynn.byedpi.ACTION_START";
    public static final String ACTION_STOP = "com.vlynn.byedpi.ACTION_STOP";
    public static final String ACTION_STATUS_BROADCAST = "com.vlynn.byedpi.STATUS_CHANGED";

    public static final String EXTRA_MODE = "extra_mode";
    public static final String EXTRA_CUSTOM_DNS = "extra_custom_dns";

    private static final String CHANNEL_ID = "vlynn_byedpi_vpn_channel";
    private static final int NOTIFICATION_ID = 1001;

    private static final String VIRTUAL_CLIENT_IP = "10.215.173.1";
    private static final String VIRTUAL_DNS_IP = "10.215.173.2";

    private static final AtomicBoolean sIsRunning = new AtomicBoolean(false);
    private static volatile int sActiveMode = 0;
    private static volatile String sActiveModeTitle = "";
    private static volatile String sActiveDetail = "";
    private static volatile String sLastError = "";
    private static final AtomicLong sQueryCount = new AtomicLong(0);

    private ParcelFileDescriptor mVpnInterface;
    private Thread mTunnelThread;
    private ExecutorService mDnsWorkerPool;

    public static boolean isRunning() {
        return sIsRunning.get();
    }

    public static int getActiveMode() {
        return sIsRunning.get() ? sActiveMode : 0;
    }

    public static String getActiveModeTitle() {
        return sActiveModeTitle;
    }

    public static String getActiveDetail() {
        return sActiveDetail;
    }

    public static String getLastError() {
        return sLastError;
    }

    public static long getQueryCount() {
        return sQueryCount.get();
    }

    @Override
    public int onStartCommand(Intent intent, int flags, int startId) {
        if (intent == null || ACTION_STOP.equals(intent.getAction())) {
            stopVpnInternal();
            stopSelf();
            return START_NOT_STICKY;
        }

        int mode = intent.getIntExtra(EXTRA_MODE, 3);
        String customDns = intent.getStringExtra(EXTRA_CUSTOM_DNS);
        if (customDns == null || customDns.trim().isEmpty()) {
            SharedPreferences prefs = getSharedPreferences("vlynn_prefs", Context.MODE_PRIVATE);
            customDns = prefs.getString("custom_dns", "1.1.1.1");
        }

        startVpnWithMode(mode, customDns.trim());
        return START_STICKY;
    }

    private synchronized void startVpnWithMode(int mode, String customDnsIp) {
        stopTunnelOnly();
        sLastError = "";
        sQueryCount.set(0);

        final String upstreamIp;
        final int upstreamPort;
        final boolean useDoH;
        final String dohUrl;
        final String modeTitle;
        final String detail;

        if (mode == 1) {
            upstreamIp = "94.140.14.14";
            upstreamPort = 53;
            useDoH = true;
            dohUrl = "https://94.140.14.14/dns-query";
            modeTitle = "Mode 1 (AdBlock DoH)";
            detail = "AdGuard DoH (Encrypted) + Anti-Tracking Active";
        } else if (mode == 2) {
            upstreamIp = "77.88.8.8";
            upstreamPort = 1253;
            useDoH = false;
            dohUrl = null;
            modeTitle = "Mode 2 (Unfiltered)";
            detail = "Yandex Unfiltered DNS (Port 1253 Anti-Intercept)";
        } else if (mode == 4) {
            useDoH = false;
            dohUrl = null;
            if ("8.8.8.8".equals(customDnsIp)) {
                upstreamIp = "8.8.8.8";
                upstreamPort = 53;
                modeTitle = "Mode 4 (Google DNS)";
                detail = "Custom DNS: 8.8.8.8 (Port 53) + IPv6 Protection";
            } else if ("9.9.9.9".equals(customDnsIp)) {
                upstreamIp = "9.9.9.9";
                upstreamPort = 53;
                modeTitle = "Mode 4 (Quad9 DNS)";
                detail = "Custom DNS: 9.9.9.9 (Port 53) + Malware Block";
            } else if ("208.67.222.222".equals(customDnsIp)) {
                upstreamIp = "208.67.222.222";
                upstreamPort = 5353;
                modeTitle = "Mode 4 (OpenDNS)";
                detail = "Custom DNS: 208.67.222.222 (Non-standard Port 5353)";
            } else if ("94.140.14.14".equals(customDnsIp)) {
                upstreamIp = "94.140.14.14";
                upstreamPort = 53;
                modeTitle = "Mode 4 (AdGuard DNS)";
                detail = "Custom DNS: 94.140.14.14 (Port 53) + AdBlock";
            } else {
                upstreamIp = "1.1.1.1";
                upstreamPort = 53;
                modeTitle = "Mode 4 (Cloudflare)";
                detail = "Custom DNS: 1.1.1.1 (Port 53) + Ultra-Fast Routing";
            }
        } else {
            mode = 3;
            upstreamIp = "77.88.8.1";
            upstreamPort = 1253;
            useDoH = false;
            dohUrl = null;
            modeTitle = "Mode 3 (Gaming & Safe)";
            detail = "Low-Latency Gaming DNS (Port 1253) + Anti-Leak";
        }

        try {
            createNotificationChannel();
            Notification notification = buildNotification(modeTitle, detail);
            if (Build.VERSION.SDK_INT >= 34) {
                startForeground(NOTIFICATION_ID, notification, ServiceInfo.FOREGROUND_SERVICE_TYPE_SPECIAL_USE);
            } else {
                startForeground(NOTIFICATION_ID, notification);
            }

            Builder builder = new Builder();
            builder.setSession("VlynnByeDPI - " + modeTitle);
            builder.setMtu(1500);
            builder.addAddress(VIRTUAL_CLIENT_IP, 30);
            builder.addDnsServer(VIRTUAL_DNS_IP);
            builder.addRoute(VIRTUAL_DNS_IP, 32);

            // IPv6 Anti-Leak DNS configuration
            try {
                builder.addAddress("fd00:1:fd00:1:fd00:1:fd00:1", 126);
                if (mode == 1) {
                    builder.addDnsServer("2a10:50c0::ad1:ff");
                } else if (mode == 2) {
                    builder.addDnsServer("2a02:6b8::feed:0ff");
                } else if (mode == 3) {
                    builder.addDnsServer("2a02:6b8::feed:bad");
                } else {
                    builder.addDnsServer("2606:4700:4700::1111");
                }
            } catch (Exception ignored) {
            }

            try {
                builder.addDisallowedApplication(getPackageName());
            } catch (Exception ignored) {
            }

            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.Q) {
                builder.setMetered(false);
            }

            PendingIntent configureIntent = PendingIntent.getActivity(
                    this, 0, new Intent(this, MainActivity.class),
                    PendingIntent.FLAG_UPDATE_CURRENT | PendingIntent.FLAG_IMMUTABLE
            );
            builder.setConfigureIntent(configureIntent);

            mVpnInterface = builder.establish();
            if (mVpnInterface == null) {
                sLastError = "VPN interface could not be established (Permission denied).";
                sIsRunning.set(false);
                sActiveMode = 0;
                broadcastState();
                stopForeground(true);
                stopSelf();
                return;
            }

            sActiveMode = mode;
            sActiveModeTitle = modeTitle;
            sActiveDetail = detail;
            sIsRunning.set(true);

            SharedPreferences prefs = getSharedPreferences("vlynn_prefs", Context.MODE_PRIVATE);
            prefs.edit().putInt("last_active_mode", mode).apply();

            mDnsWorkerPool = Executors.newCachedThreadPool();
            final ParcelFileDescriptor tunFd = mVpnInterface;

            mTunnelThread = new Thread(new Runnable() {
                @Override
                public void run() {
                    runDnsTunnelLoop(tunFd, upstreamIp, upstreamPort, useDoH, dohUrl);
                }
            }, "VlynnDnsTunnelThread");
            mTunnelThread.start();

            broadcastState();
        } catch (Exception e) {
            sLastError = "Error starting VPN: " + e.getMessage();
            sIsRunning.set(false);
            sActiveMode = 0;
            broadcastState();
            stopVpnInternal();
        }
    }

    private void runDnsTunnelLoop(ParcelFileDescriptor tunFd, final String upstreamIp, final int upstreamPort,
                                  final boolean useDoH, final String dohUrl) {
        final FileInputStream in = new FileInputStream(tunFd.getFileDescriptor());
        final FileOutputStream out = new FileOutputStream(tunFd.getFileDescriptor());
        byte[] packetBuffer = new byte[32767];

        try {
            while (sIsRunning.get() && !Thread.currentThread().isInterrupted()) {
                int length = in.read(packetBuffer);
                if (length <= 0) {
                    continue;
                }

                // Parse IPv4 UDP Packet
                int version = (packetBuffer[0] >> 4) & 0x0F;
                if (version != 4 || length < 28) {
                    continue;
                }

                int ihl = (packetBuffer[0] & 0x0F) * 4;
                int protocol = packetBuffer[9] & 0xFF;
                if (protocol != 17 || length < ihl + 8) { // 17 = UDP
                    continue;
                }

                final byte[] reqPacket = new byte[length];
                System.arraycopy(packetBuffer, 0, reqPacket, 0, length);
                final int ipHeaderLen = ihl;

                ExecutorService pool = mDnsWorkerPool;
                if (pool != null && !pool.isShutdown()) {
                    pool.execute(new Runnable() {
                        @Override
                        public void run() {
                            handleUdpDnsPacket(reqPacket, ipHeaderLen, out, upstreamIp, upstreamPort, useDoH, dohUrl);
                        }
                    });
                }
            }
        } catch (Exception ignored) {
        }
    }

    private void handleUdpDnsPacket(byte[] reqPacket, int ipHeaderLen, FileOutputStream tunOut,
                                    String upstreamIp, int upstreamPort, boolean useDoH, String dohUrl) {
        try {
            int udpTotalLen = ((reqPacket[ipHeaderLen + 4] & 0xFF) << 8) | (reqPacket[ipHeaderLen + 5] & 0xFF);
            int dnsPayloadLen = udpTotalLen - 8;
            if (dnsPayloadLen <= 0 || ipHeaderLen + 8 + dnsPayloadLen > reqPacket.length) {
                return;
            }

            byte[] dnsQuery = new byte[dnsPayloadLen];
            System.arraycopy(reqPacket, ipHeaderLen + 8, dnsQuery, 0, dnsPayloadLen);

            byte[] dnsResponse = null;
            if (useDoH && dohUrl != null) {
                dnsResponse = queryDoH(dohUrl, dnsQuery);
            }
            if (dnsResponse == null) {
                dnsResponse = queryUdpUpstream(upstreamIp, upstreamPort, dnsQuery);
            }
            // Fallback to Cloudflare 1.1.1.1:53 if non-standard port is blocked on current Wi-Fi/carrier
            if (dnsResponse == null && (!"1.1.1.1".equals(upstreamIp) || upstreamPort != 53)) {
                dnsResponse = queryUdpUpstream("1.1.1.1", 53, dnsQuery);
            }

            if (dnsResponse == null || dnsResponse.length == 0) {
                return;
            }

            byte[] ipResponse = buildIpv4UdpResponse(reqPacket, ipHeaderLen, dnsResponse);
            synchronized (tunOut) {
                tunOut.write(ipResponse);
                tunOut.flush();
            }

            long count = sQueryCount.incrementAndGet();
            if (count == 1 || count % 5 == 0) {
                broadcastState();
            }
        } catch (Exception ignored) {
        }
    }

    private byte[] queryDoH(String dohUrl, byte[] dnsQuery) {
        HttpURLConnection conn = null;
        try {
            URL url = new URL(dohUrl);
            conn = (HttpURLConnection) url.openConnection();
            conn.setRequestMethod("POST");
            conn.setConnectTimeout(2500);
            conn.setReadTimeout(2500);
            conn.setDoOutput(true);
            conn.setDoInput(true);
            conn.setRequestProperty("Content-Type", "application/dns-message");
            conn.setRequestProperty("Accept", "application/dns-message");

            OutputStream os = conn.getOutputStream();
            os.write(dnsQuery);
            os.flush();
            os.close();

            if (conn.getResponseCode() == 200) {
                InputStream is = conn.getInputStream();
                ByteArrayOutputStream buffer = new ByteArrayOutputStream();
                byte[] chunk = new byte[1024];
                int read;
                while ((read = is.read(chunk)) != -1) {
                    buffer.write(chunk, 0, read);
                }
                is.close();
                return buffer.toByteArray();
            }
        } catch (Exception ignored) {
        } finally {
            if (conn != null) {
                conn.disconnect();
            }
        }
        return null;
    }

    private byte[] queryUdpUpstream(String upstreamIp, int upstreamPort, byte[] dnsQuery) {
        DatagramSocket socket = null;
        try {
            socket = new DatagramSocket();
            protect(socket);
            socket.setSoTimeout(2200);

            InetAddress address = InetAddress.getByName(upstreamIp);
            DatagramPacket sendPacket = new DatagramPacket(dnsQuery, dnsQuery.length, address, upstreamPort);
            socket.send(sendPacket);

            byte[] respBuf = new byte[4096];
            DatagramPacket recvPacket = new DatagramPacket(respBuf, respBuf.length);
            socket.receive(recvPacket);

            byte[] result = new byte[recvPacket.getLength()];
            System.arraycopy(respBuf, 0, result, 0, recvPacket.getLength());
            return result;
        } catch (Exception ignored) {
            return null;
        } finally {
            if (socket != null) {
                socket.close();
            }
        }
    }

    private byte[] buildIpv4UdpResponse(byte[] reqPacket, int reqIpHeaderLen, byte[] dnsPayload) {
        int udpLen = 8 + dnsPayload.length;
        int totalLen = 20 + udpLen;
        byte[] out = new byte[totalLen];

        // IPv4 Header (20 bytes)
        out[0] = 0x45; // Version 4, IHL 5 (20 bytes)
        out[1] = 0x00;
        out[2] = (byte) ((totalLen >> 8) & 0xFF);
        out[3] = (byte) (totalLen & 0xFF);
        out[4] = reqPacket[4];
        out[5] = reqPacket[5];
        out[6] = 0x40; // Don't Fragment
        out[7] = 0x00;
        out[8] = 64;   // TTL
        out[9] = 17;   // Protocol UDP

        // Swap Source IP and Destination IP
        System.arraycopy(reqPacket, 16, out, 12, 4); // Src IP = original Dst IP
        System.arraycopy(reqPacket, 12, out, 16, 4); // Dst IP = original Src IP

        // Compute IPv4 Header Checksum
        int sum = 0;
        for (int i = 0; i < 20; i += 2) {
            int word = ((out[i] & 0xFF) << 8) | (out[i + 1] & 0xFF);
            sum += word;
        }
        while ((sum >> 16) != 0) {
            sum = (sum & 0xFFFF) + (sum >> 16);
        }
        int checksum = ~sum & 0xFFFF;
        out[10] = (byte) ((checksum >> 8) & 0xFF);
        out[11] = (byte) (checksum & 0xFF);

        // UDP Header (8 bytes) - Swap Source Port and Destination Port
        out[20] = reqPacket[reqIpHeaderLen + 2];
        out[21] = reqPacket[reqIpHeaderLen + 3];
        out[22] = reqPacket[reqIpHeaderLen];
        out[23] = reqPacket[reqIpHeaderLen + 1];
        out[24] = (byte) ((udpLen >> 8) & 0xFF);
        out[25] = (byte) (udpLen & 0xFF);
        out[26] = 0; // UDP checksum optional in IPv4 (0 = disabled)
        out[27] = 0;

        // DNS Payload
        System.arraycopy(dnsPayload, 0, out, 28, dnsPayload.length);
        return out;
    }

    private void stopTunnelOnly() {
        sIsRunning.set(false);
        if (mTunnelThread != null) {
            mTunnelThread.interrupt();
            mTunnelThread = null;
        }
        if (mDnsWorkerPool != null) {
            mDnsWorkerPool.shutdownNow();
            mDnsWorkerPool = null;
        }
        if (mVpnInterface != null) {
            try {
                mVpnInterface.close();
            } catch (Exception ignored) {
            }
            mVpnInterface = null;
        }
    }

    private synchronized void stopVpnInternal() {
        stopTunnelOnly();
        sActiveMode = 0;
        sActiveModeTitle = "";
        sActiveDetail = "Protection stopped. Select a mode to start.";
        broadcastState();
        try {
            stopForeground(true);
        } catch (Exception ignored) {
        }
    }

    private void broadcastState() {
        Intent b = new Intent(ACTION_STATUS_BROADCAST);
        b.setPackage(getPackageName());
        sendBroadcast(b);
    }

    private void createNotificationChannel() {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            NotificationChannel channel = new NotificationChannel(
                    CHANNEL_ID,
                    "VlynnByeDPI Protection Status",
                    NotificationManager.IMPORTANCE_LOW
            );
            channel.setDescription("Shows active DPI Bypass and Secure DNS status");
            NotificationManager nm = getSystemService(NotificationManager.class);
            if (nm != null) {
                nm.createNotificationChannel(channel);
            }
        }
    }

    private Notification buildNotification(String modeTitle, String detail) {
        Intent openIntent = new Intent(this, MainActivity.class);
        PendingIntent contentPending = PendingIntent.getActivity(
                this, 0, openIntent,
                PendingIntent.FLAG_UPDATE_CURRENT | PendingIntent.FLAG_IMMUTABLE
        );

        Intent stopIntent = new Intent(this, VlynnVpnService.class);
        stopIntent.setAction(ACTION_STOP);
        PendingIntent stopPending = PendingIntent.getService(
                this, 1, stopIntent,
                PendingIntent.FLAG_UPDATE_CURRENT | PendingIntent.FLAG_IMMUTABLE
        );

        Notification.Builder nb;
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            nb = new Notification.Builder(this, CHANNEL_ID);
        } else {
            nb = new Notification.Builder(this);
        }

        nb.setContentTitle("VlynnByeDPI Active — " + modeTitle)
          .setContentText(detail)
          .setSmallIcon(android.R.drawable.ic_lock_lock)
          .setContentIntent(contentPending)
          .setOngoing(true)
          .addAction(new Notification.Action.Builder(
                  android.R.drawable.ic_media_pause, "STOP", stopPending).build());

        return nb.build();
    }

    @Override
    public void onRevoke() {
        stopVpnInternal();
        super.onRevoke();
    }

    @Override
    public void onDestroy() {
        stopVpnInternal();
        super.onDestroy();
    }
}


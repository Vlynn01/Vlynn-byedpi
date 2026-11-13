package com.vlynn.byedpi;

import android.content.BroadcastReceiver;
import android.content.Context;
import android.content.Intent;
import android.content.SharedPreferences;
import android.net.VpnService;
import android.os.Build;

public class BootReceiver extends BroadcastReceiver {
    @Override
    public void onReceive(Context context, Intent intent) {
        if (intent == null || intent.getAction() == null) return;

        SharedPreferences prefs = context.getSharedPreferences("vlynn_prefs", Context.MODE_PRIVATE);
        boolean autoConnect = prefs.getBoolean("auto_connect", false);
        if (!autoConnect) return;

        // Only auto-start if VPN permission was already granted previously
        if (VpnService.prepare(context) == null) {
            int mode = prefs.getInt("startup_mode", 3);
            String customDns = prefs.getString("custom_dns", "1.1.1.1");

            Intent serviceIntent = new Intent(context, VlynnVpnService.class);
            serviceIntent.setAction(VlynnVpnService.ACTION_START);
            serviceIntent.putExtra(VlynnVpnService.EXTRA_MODE, mode);
            serviceIntent.putExtra(VlynnVpnService.EXTRA_CUSTOM_DNS, customDns);

            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
                context.startForegroundService(serviceIntent);
            } else {
                context.startService(serviceIntent);
            }
        }
    }
}


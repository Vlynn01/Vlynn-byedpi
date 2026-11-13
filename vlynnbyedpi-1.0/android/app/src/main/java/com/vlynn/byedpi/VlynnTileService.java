package com.vlynn.byedpi;

import android.app.PendingIntent;
import android.content.Context;
import android.content.Intent;
import android.content.SharedPreferences;
import android.net.VpnService;
import android.os.Build;
import android.service.quicksettings.Tile;
import android.service.quicksettings.TileService;

public class VlynnTileService extends TileService {

    @Override
    public void onStartListening() {
        super.onStartListening();
        syncTileState();
    }

    @Override
    @SuppressWarnings("deprecation")
    public void onClick() {
        super.onClick();
        if (VlynnVpnService.isRunning()) {
            Intent stopIntent = new Intent(this, VlynnVpnService.class);
            stopIntent.setAction(VlynnVpnService.ACTION_STOP);
            startService(stopIntent);
        } else {
            if (VpnService.prepare(this) != null) {
                Intent openApp = new Intent(this, MainActivity.class);
                openApp.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK);
                if (Build.VERSION.SDK_INT >= 34) {
                    PendingIntent pi = PendingIntent.getActivity(
                            this, 0, openApp,
                            PendingIntent.FLAG_UPDATE_CURRENT | PendingIntent.FLAG_IMMUTABLE
                    );
                    startActivityAndCollapse(pi);
                } else {
                    startActivityAndCollapse(openApp);
                }
                return;
            }
            SharedPreferences prefs = getSharedPreferences("vlynn_prefs", Context.MODE_PRIVATE);
            int mode = prefs.getInt("last_active_mode", 3);
            String customDns = prefs.getString("custom_dns", "1.1.1.1");

            Intent startIntent = new Intent(this, VlynnVpnService.class);
            startIntent.setAction(VlynnVpnService.ACTION_START);
            startIntent.putExtra(VlynnVpnService.EXTRA_MODE, mode);
            startIntent.putExtra(VlynnVpnService.EXTRA_CUSTOM_DNS, customDns);
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
                startForegroundService(startIntent);
            } else {
                startService(startIntent);
            }
        }
        syncTileState();
    }

    private void syncTileState() {
        Tile tile = getQsTile();
        if (tile == null) return;
        boolean running = VlynnVpnService.isRunning();
        tile.setState(running ? Tile.STATE_ACTIVE : Tile.STATE_INACTIVE);
        tile.setLabel("VlynnByeDPI");
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.Q) {
            tile.setSubtitle(running ? VlynnVpnService.getActiveModeTitle() : "OFF");
        }
        tile.updateTile();
    }
}

package com.vlynn.byedpi;

import android.Manifest;
import android.app.Activity;
import android.app.AlertDialog;
import android.content.BroadcastReceiver;
import android.content.Context;
import android.content.DialogInterface;
import android.content.Intent;
import android.content.IntentFilter;
import android.content.SharedPreferences;
import android.content.pm.PackageManager;
import android.graphics.Color;
import android.graphics.Typeface;
import android.graphics.drawable.GradientDrawable;
import android.net.VpnService;
import android.os.Build;
import android.os.Bundle;
import android.os.Handler;
import android.os.Looper;
import android.util.TypedValue;
import android.view.Gravity;
import android.view.View;
import android.view.Window;
import android.view.WindowInsets;
import android.view.WindowManager;
import android.widget.Button;
import android.widget.CheckBox;
import android.widget.LinearLayout;
import android.widget.ScrollView;
import android.widget.Spinner;
import android.widget.ArrayAdapter;
import android.widget.TextView;

public class MainActivity extends Activity {

    private static final int REQ_VPN_PERMISSION = 100;
    private static final int REQ_NOTIF_PERMISSION = 101;

    private static final int COLOR_BG_DARK = Color.rgb(18, 20, 28);
    private static final int COLOR_CARD_BG = Color.rgb(26, 30, 42);
    private static final int COLOR_BTN_NORMAL = Color.rgb(34, 39, 54);
    private static final int COLOR_BTN_ACTIVE = Color.rgb(30, 48, 62);
    private static final int COLOR_BTN_BORDER = Color.rgb(55, 63, 84);
    private static final int COLOR_STOP_BG = Color.rgb(190, 42, 55);

    private LinearLayout mStatusCard;
    private TextView mStatusTitle;
    private TextView mStatusSubtitle;
    private TextView mCounterBadge;

    private Button mBtnMode1;
    private Button mBtnMode2;
    private Button mBtnMode3;
    private Button mBtnMode4;
    private Button mBtnStop;

    private int mPendingMode = 3;
    private String mPendingCustomDns = "1.1.1.1";

    private final Handler mUiHandler = new Handler(Looper.getMainLooper());
    private boolean mReceiverRegistered = false;

    private final String[] mModeLabels = new String[]{
            "1. AdBlock DNS & Bypass (DoH)",
            "2. Unfiltered DNS (Max Compatibility)",
            "3. Gaming & Safe DNS (Recommended)",
            "4. Custom DNS (Power User)"
    };

    private final BroadcastReceiver mStatusReceiver = new BroadcastReceiver() {
        @Override
        public void onReceive(Context context, Intent intent) {
            refreshUiFromServiceState();
        }
    };

    private final Runnable mTicker = new Runnable() {
        @Override
        public void run() {
            if (VlynnVpnService.isRunning()) {
                long q = VlynnVpnService.getQueryCount();
                mCounterBadge.setVisibility(View.VISIBLE);
                mCounterBadge.setText("Protected DNS Queries: " + q + "  •  Zero-Lag Split Tunnel");
                mUiHandler.postDelayed(this, 1500);
            } else {
                mCounterBadge.setVisibility(View.GONE);
            }
        }
    };

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);

        configureSystemBars();
        buildDashboardUi();
        requestNotificationPermissionIfNeeded();
        refreshUiFromServiceState();

        SharedPreferences prefs = getSharedPreferences("vlynn_prefs", Context.MODE_PRIVATE);
        boolean autoConnect = prefs.getBoolean("auto_connect", false);
        boolean firstLaunchHandled = savedInstanceState != null;
        if (autoConnect && !firstLaunchHandled && !VlynnVpnService.isRunning()) {
            int startupMode = prefs.getInt("startup_mode", 3);
            String savedDns = prefs.getString("custom_dns", "1.1.1.1");
            prepareAndStartMode(startupMode, savedDns);
        }
    }

    private void configureSystemBars() {
        Window window = getWindow();
        if (window != null) {
            window.addFlags(WindowManager.LayoutParams.FLAG_DRAWS_SYSTEM_BAR_BACKGROUNDS);
            window.setStatusBarColor(COLOR_BG_DARK);
            window.setNavigationBarColor(COLOR_BG_DARK);
        }
    }

    private void requestNotificationPermissionIfNeeded() {
        if (Build.VERSION.SDK_INT >= 33) {
            if (checkSelfPermission(Manifest.permission.POST_NOTIFICATIONS) != PackageManager.PERMISSION_GRANTED) {
                requestPermissions(new String[]{Manifest.permission.POST_NOTIFICATIONS}, REQ_NOTIF_PERMISSION);
            }
        }
    }

    private void buildDashboardUi() {
        ScrollView scrollView = new ScrollView(this);
        scrollView.setBackgroundColor(COLOR_BG_DARK);
        scrollView.setFillViewport(true);

        final LinearLayout root = new LinearLayout(this);
        root.setOrientation(LinearLayout.VERTICAL);
        root.setBackgroundColor(COLOR_BG_DARK);
        final int basePadH = dp(22);
        final int basePadV = dp(20);
        root.setPadding(basePadH, basePadV, basePadH, basePadV);

        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.R) {
            root.setOnApplyWindowInsetsListener(new View.OnApplyWindowInsetsListener() {
                @Override
                public WindowInsets onApplyWindowInsets(View v, WindowInsets insets) {
                    int top = insets.getInsets(WindowInsets.Type.systemBars()).top;
                    int bottom = insets.getInsets(WindowInsets.Type.systemBars()).bottom;
                    v.setPadding(basePadH, basePadV + top, basePadH, basePadV + bottom);
                    return insets;
                }
            });
        }

        // Header Row (Title + Settings Button)
        LinearLayout headerRow = new LinearLayout(this);
        headerRow.setOrientation(LinearLayout.HORIZONTAL);
        headerRow.setGravity(Gravity.CENTER_VERTICAL);

        LinearLayout titleCol = new LinearLayout(this);
        titleCol.setOrientation(LinearLayout.VERTICAL);
        LinearLayout.LayoutParams titleColParams = new LinearLayout.LayoutParams(0, LinearLayout.LayoutParams.WRAP_CONTENT, 1f);
        titleCol.setLayoutParams(titleColParams);

        TextView appTitle = new TextView(this);
        appTitle.setText("VlynnByeDPI v1.1");
        appTitle.setTextColor(Color.WHITE);
        appTitle.setTextSize(TypedValue.COMPLEX_UNIT_SP, 24);
        appTitle.setTypeface(Typeface.create("sans-serif-medium", Typeface.BOLD));
        titleCol.addView(appTitle);

        TextView appSub = new TextView(this);
        appSub.setText("Android 16  •  DPI Bypass & Secure DNS Engine");
        appSub.setTextColor(Color.rgb(145, 155, 175));
        appSub.setTextSize(TypedValue.COMPLEX_UNIT_SP, 12.5f);
        titleCol.addView(appSub);

        headerRow.addView(titleCol);

        Button btnSettings = new Button(this);
        btnSettings.setText("⚙");
        btnSettings.setTextColor(Color.WHITE);
        btnSettings.setTextSize(TypedValue.COMPLEX_UNIT_SP, 18);
        btnSettings.setAllCaps(false);
        btnSettings.setPadding(0, 0, 0, 0);
        btnSettings.setBackground(makeRoundedRect(COLOR_BTN_NORMAL, COLOR_BTN_BORDER, dp(10)));
        LinearLayout.LayoutParams settingsParams = new LinearLayout.LayoutParams(dp(44), dp(44));
        btnSettings.setLayoutParams(settingsParams);
        btnSettings.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                showSettingsDialog();
            }
        });
        headerRow.addView(btnSettings);

        root.addView(headerRow);

        // Status Card
        mStatusCard = new LinearLayout(this);
        mStatusCard.setOrientation(LinearLayout.VERTICAL);
        mStatusCard.setGravity(Gravity.CENTER);
        mStatusCard.setPadding(dp(16), dp(18), dp(16), dp(18));
        mStatusCard.setBackground(makeRoundedRect(COLOR_CARD_BG, COLOR_BTN_BORDER, dp(14)));
        LinearLayout.LayoutParams cardParams = new LinearLayout.LayoutParams(
                LinearLayout.LayoutParams.MATCH_PARENT,
                LinearLayout.LayoutParams.WRAP_CONTENT
        );
        cardParams.topMargin = dp(20);
        cardParams.bottomMargin = dp(20);
        mStatusCard.setLayoutParams(cardParams);

        mStatusTitle = new TextView(this);
        mStatusTitle.setText("● Status: OFF");
        mStatusTitle.setTextColor(Color.rgb(150, 158, 175));
        mStatusTitle.setTextSize(TypedValue.COMPLEX_UNIT_SP, 16);
        mStatusTitle.setTypeface(Typeface.DEFAULT_BOLD);
        mStatusTitle.setGravity(Gravity.CENTER);
        mStatusCard.addView(mStatusTitle);

        mStatusSubtitle = new TextView(this);
        mStatusSubtitle.setText("Select a mode below to activate protection");
        mStatusSubtitle.setTextColor(Color.rgb(130, 138, 155));
        mStatusSubtitle.setTextSize(TypedValue.COMPLEX_UNIT_SP, 13);
        mStatusSubtitle.setGravity(Gravity.CENTER);
        LinearLayout.LayoutParams subParams = new LinearLayout.LayoutParams(
                LinearLayout.LayoutParams.MATCH_PARENT,
                LinearLayout.LayoutParams.WRAP_CONTENT
        );
        subParams.topMargin = dp(6);
        mStatusSubtitle.setLayoutParams(subParams);
        mStatusCard.addView(mStatusSubtitle);

        mCounterBadge = new TextView(this);
        mCounterBadge.setTextColor(Color.rgb(175, 195, 220));
        mCounterBadge.setTextSize(TypedValue.COMPLEX_UNIT_SP, 11.5f);
        mCounterBadge.setGravity(Gravity.CENTER);
        mCounterBadge.setVisibility(View.GONE);
        LinearLayout.LayoutParams counterParams = new LinearLayout.LayoutParams(
                LinearLayout.LayoutParams.MATCH_PARENT,
                LinearLayout.LayoutParams.WRAP_CONTENT
        );
        counterParams.topMargin = dp(8);
        mCounterBadge.setLayoutParams(counterParams);
        mStatusCard.addView(mCounterBadge);

        root.addView(mStatusCard);

        // Mode Buttons
        mBtnMode1 = createModeButton(mModeLabels[0], new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                prepareAndStartMode(1, null);
            }
        });
        root.addView(mBtnMode1);

        mBtnMode2 = createModeButton(mModeLabels[1], new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                prepareAndStartMode(2, null);
            }
        });
        root.addView(mBtnMode2);

        mBtnMode3 = createModeButton(mModeLabels[2], new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                prepareAndStartMode(3, null);
            }
        });
        root.addView(mBtnMode3);

        mBtnMode4 = createModeButton(mModeLabels[3], new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                showCustomDnsDialog();
            }
        });
        root.addView(mBtnMode4);

        // STOP Button (Strictly hidden until VPN is verified RUNNING)
        mBtnStop = new Button(this);
        mBtnStop.setText("■   STOP PROTECTION");
        mBtnStop.setAllCaps(false);
        mBtnStop.setTextColor(Color.WHITE);
        mBtnStop.setTextSize(TypedValue.COMPLEX_UNIT_SP, 15);
        mBtnStop.setTypeface(Typeface.DEFAULT_BOLD);
        mBtnStop.setBackground(makeRoundedRect(COLOR_STOP_BG, Color.rgb(235, 85, 95), dp(12)));
        LinearLayout.LayoutParams stopParams = new LinearLayout.LayoutParams(
                LinearLayout.LayoutParams.MATCH_PARENT,
                dp(54)
        );
        stopParams.topMargin = dp(18);
        mBtnStop.setLayoutParams(stopParams);
        mBtnStop.setVisibility(View.GONE);
        mBtnStop.setEnabled(false);
        mBtnStop.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                stopVpnProtection();
            }
        });
        root.addView(mBtnStop);

        // Footer Note
        TextView footer = new TextView(this);
        footer.setText("Developed exclusively by Vlynn  •  Android 16 (API 36) Ready");
        footer.setTextColor(Color.rgb(95, 105, 125));
        footer.setTextSize(TypedValue.COMPLEX_UNIT_SP, 11.5f);
        footer.setGravity(Gravity.CENTER);
        LinearLayout.LayoutParams footerParams = new LinearLayout.LayoutParams(
                LinearLayout.LayoutParams.MATCH_PARENT,
                LinearLayout.LayoutParams.WRAP_CONTENT
        );
        footerParams.topMargin = dp(24);
        footer.setLayoutParams(footerParams);
        root.addView(footer);

        scrollView.addView(root);
        setContentView(scrollView);
    }

    private Button createModeButton(String text, View.OnClickListener listener) {
        Button btn = new Button(this);
        btn.setText(text);
        btn.setAllCaps(false);
        btn.setTextColor(Color.rgb(235, 240, 250));
        btn.setTextSize(TypedValue.COMPLEX_UNIT_SP, 14.5f);
        btn.setGravity(Gravity.CENTER_VERTICAL | Gravity.START);
        btn.setPadding(dp(18), 0, dp(18), 0);
        btn.setBackground(makeRoundedRect(COLOR_BTN_NORMAL, COLOR_BTN_BORDER, dp(12)));

        LinearLayout.LayoutParams lp = new LinearLayout.LayoutParams(
                LinearLayout.LayoutParams.MATCH_PARENT,
                dp(54)
        );
        lp.bottomMargin = dp(12);
        btn.setLayoutParams(lp);
        btn.setOnClickListener(listener);
        return btn;
    }

    private GradientDrawable makeRoundedRect(int fillColor, int strokeColor, int radiusPx) {
        GradientDrawable gd = new GradientDrawable();
        gd.setShape(GradientDrawable.RECTANGLE);
        gd.setColor(fillColor);
        gd.setCornerRadius(radiusPx);
        gd.setStroke(dp(1), strokeColor);
        return gd;
    }

    private void prepareAndStartMode(int mode, String customDns) {
        mPendingMode = mode;
        if (customDns != null) {
            mPendingCustomDns = customDns;
        }

        // Show transitional feedback
        mStatusTitle.setText("⚡ Activating Mode " + mode + "...");
        mStatusTitle.setTextColor(Color.rgb(255, 180, 50));
        mStatusSubtitle.setText("Initializing Android 16 Secure DNS & DPI Tunnel...");
        mStatusCard.setBackground(makeRoundedRect(COLOR_CARD_BG, Color.rgb(230, 150, 40), dp(14)));

        Intent prepareIntent = VpnService.prepare(this);
        if (prepareIntent != null) {
            startActivityForResult(prepareIntent, REQ_VPN_PERMISSION);
        } else {
            onActivityResult(REQ_VPN_PERMISSION, Activity.RESULT_OK, null);
        }
    }

    @Override
    protected void onActivityResult(int requestCode, int resultCode, Intent data) {
        super.onActivityResult(requestCode, resultCode, data);
        if (requestCode == REQ_VPN_PERMISSION) {
            if (resultCode == Activity.RESULT_OK) {
                Intent serviceIntent = new Intent(this, VlynnVpnService.class);
                serviceIntent.setAction(VlynnVpnService.ACTION_START);
                serviceIntent.putExtra(VlynnVpnService.EXTRA_MODE, mPendingMode);
                serviceIntent.putExtra(VlynnVpnService.EXTRA_CUSTOM_DNS, mPendingCustomDns);

                if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
                    startForegroundService(serviceIntent);
                } else {
                    startService(serviceIntent);
                }

                mUiHandler.postDelayed(new Runnable() {
                    @Override
                    public void run() {
                        refreshUiFromServiceState();
                    }
                }, 300);
            } else {
                // User denied VPN dialog -> revert to OFF and keep STOP button hidden
                mStatusTitle.setText("⚠ VPN Permission Cancelled");
                mStatusTitle.setTextColor(Color.rgb(255, 95, 95));
                mStatusSubtitle.setText("Please allow the VPN connection prompt to activate protection.");
                mStatusCard.setBackground(makeRoundedRect(COLOR_CARD_BG, Color.rgb(220, 80, 80), dp(14)));
                mBtnStop.setVisibility(View.GONE);
                mBtnStop.setEnabled(false);
                highlightActiveMode(0, Color.GRAY);
            }
        }
    }

    private void stopVpnProtection() {
        if (!VlynnVpnService.isRunning()) {
            refreshUiFromServiceState();
            return;
        }

        Intent stopIntent = new Intent(this, VlynnVpnService.class);
        stopIntent.setAction(VlynnVpnService.ACTION_STOP);
        startService(stopIntent);

        mUiHandler.postDelayed(new Runnable() {
            @Override
            public void run() {
                refreshUiFromServiceState();
            }
        }, 150);
    }

    private void refreshUiFromServiceState() {
        boolean running = VlynnVpnService.isRunning();
        int activeMode = VlynnVpnService.getActiveMode();
        String lastErr = VlynnVpnService.getLastError();

        if (running && activeMode >= 1 && activeMode <= 4) {
            int accent;
            if (activeMode == 1) accent = Color.rgb(46, 204, 113);
            else if (activeMode == 2) accent = Color.rgb(255, 120, 70);
            else if (activeMode == 3) accent = Color.rgb(0, 205, 255);
            else accent = Color.rgb(240, 100, 185);

            mStatusTitle.setText("● RUNNING — " + VlynnVpnService.getActiveModeTitle());
            mStatusTitle.setTextColor(accent);
            mStatusSubtitle.setText(VlynnVpnService.getActiveDetail());
            mStatusSubtitle.setTextColor(Color.rgb(195, 205, 220));
            mStatusCard.setBackground(makeRoundedRect(COLOR_CARD_BG, accent, dp(14)));

            highlightActiveMode(activeMode, accent);

            // Show and enable STOP button ONLY when verified running
            mBtnStop.setVisibility(View.VISIBLE);
            mBtnStop.setEnabled(true);

            mUiHandler.removeCallbacks(mTicker);
            mUiHandler.post(mTicker);
        } else {
            mUiHandler.removeCallbacks(mTicker);
            mCounterBadge.setVisibility(View.GONE);

            if (lastErr != null && !lastErr.isEmpty()) {
                mStatusTitle.setText("⚠ FAILED TO START");
                mStatusTitle.setTextColor(Color.rgb(255, 95, 95));
                mStatusSubtitle.setText(lastErr);
                mStatusSubtitle.setTextColor(Color.rgb(235, 140, 140));
                mStatusCard.setBackground(makeRoundedRect(COLOR_CARD_BG, Color.rgb(220, 80, 80), dp(14)));
            } else {
                mStatusTitle.setText("● Status: OFF");
                mStatusTitle.setTextColor(Color.rgb(150, 158, 175));
                String detail = VlynnVpnService.getActiveDetail();
                mStatusSubtitle.setText((detail == null || detail.isEmpty())
                        ? "Select a mode below to activate protection"
                        : detail);
                mStatusSubtitle.setTextColor(Color.rgb(130, 138, 155));
                mStatusCard.setBackground(makeRoundedRect(COLOR_CARD_BG, COLOR_BTN_BORDER, dp(14)));
            }

            highlightActiveMode(0, Color.GRAY);

            // Strictly hide and disable STOP button when not running
            mBtnStop.setVisibility(View.GONE);
            mBtnStop.setEnabled(false);
        }
    }

    private void highlightActiveMode(int activeMode, int accentColor) {
        Button[] buttons = new Button[]{mBtnMode1, mBtnMode2, mBtnMode3, mBtnMode4};
        for (int i = 0; i < buttons.length; i++) {
            Button btn = buttons[i];
            boolean isActive = (i + 1) == activeMode;
            if (isActive) {
                btn.setText("✓   " + mModeLabels[i]);
                btn.setTextColor(accentColor);
                btn.setTypeface(Typeface.DEFAULT_BOLD);
                btn.setBackground(makeRoundedRect(COLOR_BTN_ACTIVE, accentColor, dp(12)));
            } else {
                btn.setText(mModeLabels[i]);
                btn.setTextColor(Color.rgb(235, 240, 250));
                btn.setTypeface(Typeface.DEFAULT);
                btn.setBackground(makeRoundedRect(COLOR_BTN_NORMAL, COLOR_BTN_BORDER, dp(12)));
            }
        }
    }

    private void showCustomDnsDialog() {
        final String[] labels = new String[]{
                "1.1.1.1 — Cloudflare DNS (Fastest, Port 53)",
                "8.8.8.8 — Google Public DNS (Reliable, Port 53)",
                "9.9.9.9 — Quad9 Security DNS (Malware Block, Port 53)",
                "208.67.222.222 — OpenDNS (Non-standard Port 5353)",
                "94.140.14.14 — AdGuard DNS (AdBlock, Port 53)"
        };
        final String[] ips = new String[]{"1.1.1.1", "8.8.8.8", "9.9.9.9", "208.67.222.222", "94.140.14.14"};

        final SharedPreferences prefs = getSharedPreferences("vlynn_prefs", Context.MODE_PRIVATE);
        String savedIp = prefs.getString("custom_dns", "1.1.1.1");
        int checkedIndex = 0;
        for (int i = 0; i < ips.length; i++) {
            if (ips[i].equals(savedIp)) {
                checkedIndex = i;
                break;
            }
        }

        final int[] selectedHolder = new int[]{checkedIndex};

        AlertDialog.Builder builder = new AlertDialog.Builder(this, android.R.style.Theme_DeviceDefault_Dialog_Alert);
        builder.setTitle("Mode 4 — Custom DNS");
        builder.setSingleChoiceItems(labels, checkedIndex, new DialogInterface.OnClickListener() {
            @Override
            public void onClick(DialogInterface dialog, int which) {
                selectedHolder[0] = which;
            }
        });
        builder.setPositiveButton("Connect", new DialogInterface.OnClickListener() {
            @Override
            public void onClick(DialogInterface dialog, int which) {
                String chosenIp = ips[selectedHolder[0]];
                prefs.edit().putString("custom_dns", chosenIp).apply();
                prepareAndStartMode(4, chosenIp);
            }
        });
        builder.setNegativeButton("Cancel", null);
        builder.show();
    }

    private void showSettingsDialog() {
        final SharedPreferences prefs = getSharedPreferences("vlynn_prefs", Context.MODE_PRIVATE);
        boolean autoConnect = prefs.getBoolean("auto_connect", false);
        int startupMode = prefs.getInt("startup_mode", 3);

        LinearLayout container = new LinearLayout(this);
        container.setOrientation(LinearLayout.VERTICAL);
        container.setPadding(dp(24), dp(18), dp(24), dp(10));

        final CheckBox chkAutoConnect = new CheckBox(this);
        chkAutoConnect.setText("Auto-connect on device boot / app start");
        chkAutoConnect.setChecked(autoConnect);
        chkAutoConnect.setTextSize(TypedValue.COMPLEX_UNIT_SP, 14.5f);
        container.addView(chkAutoConnect);

        TextView modeLabel = new TextView(this);
        modeLabel.setText("Default Startup Mode:");
        modeLabel.setTextSize(TypedValue.COMPLEX_UNIT_SP, 13.5f);
        LinearLayout.LayoutParams labelLp = new LinearLayout.LayoutParams(
                LinearLayout.LayoutParams.MATCH_PARENT,
                LinearLayout.LayoutParams.WRAP_CONTENT
        );
        labelLp.topMargin = dp(16);
        labelLp.bottomMargin = dp(6);
        modeLabel.setLayoutParams(labelLp);
        container.addView(modeLabel);

        final Spinner modeSpinner = new Spinner(this);
        String[] modeNames = new String[]{
                "Mode 1: AdBlock DNS & Bypass (DoH)",
                "Mode 2: Unfiltered DNS (Port 1253)",
                "Mode 3: Gaming & Safe DNS (Recommended)",
                "Mode 4: Custom DNS"
        };
        ArrayAdapter<String> adapter = new ArrayAdapter<>(this, android.R.layout.simple_spinner_dropdown_item, modeNames);
        modeSpinner.setAdapter(adapter);
        if (startupMode >= 1 && startupMode <= 4) {
            modeSpinner.setSelection(startupMode - 1);
        } else {
            modeSpinner.setSelection(2);
        }
        container.addView(modeSpinner);

        AlertDialog.Builder builder = new AlertDialog.Builder(this, android.R.style.Theme_DeviceDefault_Dialog_Alert);
        builder.setTitle("VlynnByeDPI Settings");
        builder.setView(container);
        builder.setPositiveButton("Save", new DialogInterface.OnClickListener() {
            @Override
            public void onClick(DialogInterface dialog, int which) {
                prefs.edit()
                     .putBoolean("auto_connect", chkAutoConnect.isChecked())
                     .putInt("startup_mode", modeSpinner.getSelectedItemPosition() + 1)
                     .apply();
            }
        });
        builder.setNegativeButton("Close", null);
        builder.show();
    }

    private int dp(float value) {
        return (int) TypedValue.applyDimension(
                TypedValue.COMPLEX_UNIT_DIP,
                value,
                getResources().getDisplayMetrics()
        );
    }

    @Override
    protected void onResume() {
        super.onResume();
        if (!mReceiverRegistered) {
            IntentFilter filter = new IntentFilter(VlynnVpnService.ACTION_STATUS_BROADCAST);
            if (Build.VERSION.SDK_INT >= 33) {
                registerReceiver(mStatusReceiver, filter, Context.RECEIVER_NOT_EXPORTED);
            } else {
                registerReceiver(mStatusReceiver, filter);
            }
            mReceiverRegistered = true;
        }
        refreshUiFromServiceState();
    }

    @Override
    protected void onPause() {
        super.onPause();
        mUiHandler.removeCallbacks(mTicker);
        if (mReceiverRegistered) {
            try {
                unregisterReceiver(mStatusReceiver);
            } catch (Exception ignored) {
            }
            mReceiverRegistered = false;
        }
    }
}

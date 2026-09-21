package io.ionic.starter;

import android.os.Bundle;
import android.webkit.WebView;

import com.getcapacitor.BridgeActivity;

public class MainActivity extends BridgeActivity {
    @Override
    public void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        // Habilita la depuración remota del WebView desde chrome://inspect
        WebView.setWebContentsDebuggingEnabled(true);
    }
}
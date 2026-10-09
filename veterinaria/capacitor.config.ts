import type { CapacitorConfig } from '@capacitor/cli';

const config: CapacitorConfig = {
  appId: 'io.ionic.starter',
  appName: 'veterinaria',
  webDir: 'dist',
  server: {
    androidScheme: 'http',
    cleartext: true
  },
  plugins: {
    CapacitorHttp: {
      enabled: true
    },
    LocalNotifications: {
    "smallIcon": "ic_stat_notificacion",
    "iconColor": "#0077F8"
    }
  }
};

export default config;

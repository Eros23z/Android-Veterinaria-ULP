const { execSync } = require('child_process');
const http = require('http');

const adbPath = process.env.ANDROID_HOME 
  ? `${process.env.ANDROID_HOME}/platform-tools/adb.exe` 
  : 'C:\\Users\\Admin\\AppData\\Local\\Android\\Sdk\\platform-tools\\adb.exe';

try {
  const pidOutput = execSync(`"${adbPath}" shell pidof io.ionic.starter`, { encoding: 'utf-8' }).trim();
  const pids = pidOutput.split(/\s+/);
  const pid = pids[pids.length - 1];

  if (!pid) {
    console.error('❌ La aplicación no está corriendo en el celular. Ábrela primero.');
    process.exit(1);
  }

  console.log(`📱 App detectada en el teléfono con PID: ${pid}`);
  execSync(`"${adbPath}" forward tcp:9222 localabstract:webview_devtools_remote_${pid}`);
  console.log('🔗 Puerto 9222 vinculado exitosamente.');

  setTimeout(() => {
    http.get('http://localhost:9222/json', (res) => {
      let data = '';
      res.on('data', chunk => data += chunk);
      res.on('end', () => {
        try {
          const targets = JSON.parse(data);
          const page = targets.find(t => t.type === 'page') || targets[0];
          if (page) {
            console.log(`\n✅ Conectado a: "${page.title}" (${page.url})`);
            console.log(`\n👉 En tu navegador (Opera/Chrome) puedes:`);
            console.log(`   1. Recargar chrome://inspect/#devices (o opera://inspect/#devices)`);
            console.log(`   2. O abrir directamente esta URL en una pestaña:\n`);
            console.log(`   ${page.devtoolsFrontendUrl}\n`);
          }
        } catch (err) {
          console.log('Túnel listo en http://localhost:9222');
        }
      });
    }).on('error', () => {
      console.log('Túnel listo en localhost:9222. Abre opera://inspect/#devices o chrome://inspect/#devices');
    });
  }, 500);

} catch (error) {
  console.error('Error al conectar con el dispositivo:', error.message);
}

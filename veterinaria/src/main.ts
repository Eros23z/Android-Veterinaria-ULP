import { createApp } from 'vue'
import App from './App.vue'
import router from './router';

import { IonicVue } from '@ionic/vue';

/* Core CSS required for Ionic components to work properly */
import '@ionic/vue/css/core.css';

/* Basic CSS for apps built with Ionic */
import '@ionic/vue/css/normalize.css';
import '@ionic/vue/css/structure.css';
import '@ionic/vue/css/typography.css';

/* Optional CSS utils that can be commented out */
import '@ionic/vue/css/padding.css';
import '@ionic/vue/css/float-elements.css';
import '@ionic/vue/css/text-alignment.css';
import '@ionic/vue/css/text-transformation.css';
import '@ionic/vue/css/flex-utils.css';
import '@ionic/vue/css/display.css';
import '@ionic/vue/css/palettes/dark.class.css';

/**
 * Ionic Dark Mode
 * -----------------------------------------------------
 * For more info, please see:
 * https://ionicframework.com/docs/theming/dark-mode
 */

/* @import '@ionic/vue/css/palettes/dark.always.css'; */
/* @import '@ionic/vue/css/palettes/dark.class.css'; */
//import '@ionic/vue/css/palettes/dark.system.css';

/* Theme variables */
import './theme/variables.css';
import './theme/global.css';
import { aplicar_tema_guardado } from './config/tema';
import { use_notificaciones_store } from '@/stores/notificaciones_store';
import { use_sesion_store } from '@/stores/sesion_store';
import { inicializar_notificaciones_nativas } from '@/services/notificaciones_nativas_service';

aplicar_tema_guardado();

inicializar_notificaciones_nativas({
  al_registrar: () => use_sesion_store().vincular_dispositivo(),
  al_recibir: () => {
    if (use_sesion_store().autenticado) {
      use_notificaciones_store().cargar();
    }
  },
  al_tocar: (datos: any) => {
    const codigo = datos?.pedido_codigo;
    if (codigo) {
      router.push(`/app/pedidos?buscar=${encodeURIComponent(codigo)}`);
    }
  },
});

const app = createApp(App)
  .use(IonicVue)
  .use(router);

router.isReady().then(() => {
  app.mount('#app');
});

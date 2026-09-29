<template>
  <ion-page>
    <ion-header>
      <ion-toolbar color="primary">
        <ion-title>Veterinaria San Roque</ion-title>
      </ion-toolbar>
    </ion-header>

    <ion-content class="ion-padding" :fullscreen="true">
      <div class="login-contenedor">
        <div class="logo-envoltorio">
          <ion-icon :icon="pawOutline" class="icono-logo"></ion-icon>
        </div>

        <h1 class="titulo-login">Iniciar Sesión</h1>
        <p class="subtitulo-login">Acceso seguro al sistema de gestión clínica</p>

        <!-- Mensaje de error reactivo -->
        <div v-if="errorMensaje" class="ion-margin-bottom">
          <ion-note color="danger" class="error-banner">
            <ion-icon :icon="alertCircleOutline" class="icono-error"></ion-icon>
            <span>{{ errorMensaje }}</span>
          </ion-note>
        </div>

        <form @submit.prevent="ejecutarLogin">
          <ion-list inset class="formulario-lista">
            <ion-item>
              <ion-icon slot="start" :icon="mailOutline" color="medium"></ion-icon>
              <ion-label position="stacked">Correo Electrónico</ion-label>
              <ion-input
                v-model="email"
                type="email"
                placeholder="ejemplo@veterinaria.local"
                required
                autocomplete="email"
                :disabled="cargando"
              ></ion-input>
            </ion-item>

            <ion-item>
              <ion-icon slot="start" :icon="lockClosedOutline" color="medium"></ion-icon>
              <ion-label position="stacked">Contraseña</ion-label>
              <ion-input
                v-model="password"
                type="password"
                placeholder="••••••••"
                required
                autocomplete="current-password"
                :disabled="cargando"
              ></ion-input>
            </ion-item>
          </ion-list>

          <div class="ion-padding-top">
            <ion-button
              type="submit"
              expand="block"
              shape="round"
              size="large"
              :disabled="cargando || !email || !password"
            >
              <ion-spinner v-if="cargando" name="crescent" slot="start"></ion-spinner>
              <ion-icon v-else slot="start" :icon="logInOutline"></ion-icon>
              {{ cargando ? 'Iniciando sesión...' : 'Ingresar' }}
            </ion-button>
          </div>
        </form>

        <!-- Botón para login con biometría según diseño Unidad 5 -->
        <div v-if="sesion_store.puede_usar_huella" class="seccion-biometria ion-padding-top">
          <div class="separador">
            <span>o ingresa con</span>
          </div>

          <ion-button
            v-if="sesion_store.puede_usar_huella"
            expand="block"
            fill="outline"
            shape="round"
            color="secondary"
            :disabled="cargando"
            @click="ingresarConHuella"
          >
            <ion-icon slot="start" :icon="fingerPrintOutline"></ion-icon>
            ENTRAR CON HUELLA
          </ion-button>
        </div>
      </div>
    </ion-content>
  </ion-page>
</template>

<script setup>
import { ref } from 'vue';
import { useRouter } from 'vue-router';
import {
  IonPage,
  IonHeader,
  IonToolbar,
  IonTitle,
  IonContent,
  IonList,
  IonItem,
  IonLabel,
  IonInput,
  IonButton,
  IonIcon,
  IonNote,
  IonSpinner,
} from '@ionic/vue';
import {
  pawOutline,
  mailOutline,
  lockClosedOutline,
  logInOutline,
  fingerPrintOutline,
  alertCircleOutline,
} from 'ionicons/icons';
import { sesion_store } from '@/stores/sesion_store';
import { token_service } from '@/services/token_service';
import { sesion_service } from '@/services/sesion_service';
import { biometria_service } from '@/services/biometria_service';
import { vibrar_toque, vibrar_error } from '@/services/vibracion_service';

const router = useRouter();

const email = ref('');
const password = ref('');
const cargando = ref(false);
const errorMensaje = ref(null);

async function ejecutarLogin() {
  if (!email.value || !password.value) return;

  await vibrar_toque();
  errorMensaje.value = null;
  cargando.value = true;

  try {
    await sesion_store.login(email.value.trim(), password.value);
    await vibrar_toque();
    router.replace('/app/inicio');
  } catch (error) {
    await vibrar_error();
    errorMensaje.value =
      error.mensaje || error.message || 'No se pudo iniciar sesión. Verifique sus credenciales.';
  } finally {
    cargando.value = false;
  }
}

async function ingresarConHuella() {
  errorMensaje.value = null;

  // 1. Llama a biometria_service.autenticar("Confirma tu identidad para entrar")
  const autorizada = await biometria_service.autenticar('Confirma tu identidad para entrar');

  // 2. Si la huella es rechazada o cancelada por el usuario: permanece en la pantalla de login sin hacer nada
  if (!autorizada) {
    return;
  }

  // 3. Si la huella es exitosa:
  cargando.value = true;
  try {
    // Recupera el token en memoria
    const refreshToken = token_service.obtener_refresh_token();

    let perfil = null;
    try {
      // Llama a GET /api/sesion/yo para comprobar que la sesión y el rol sigan vigentes
      perfil = await sesion_service.obtener_perfil();
    } catch (err) {
      // Si da 401, intenta un refresh
      if (err.status === 401 && refreshToken) {
        try {
          const resRefresh = await sesion_service.refresh(refreshToken);
          await token_service.guardar_tokens(resRefresh.token, resRefresh.refresh_token);
          perfil = resRefresh.usuario;
        } catch (refreshErr) {
          // Si el refresh falla, limpia tokens y pide contraseña
          await token_service.limpiar_tokens();
          sesion_store.hay_token_guardado = false;
          sesion_store.puede_usar_huella = false;
          sesion_store._limpiar_estado();
          throw new Error('La sesión guardada expiró o fue revocada. Ingrese con su contraseña.');
        }
      } else {
        await token_service.limpiar_tokens();
        sesion_store.hay_token_guardado = false;
        sesion_store.puede_usar_huella = false;
        sesion_store._limpiar_estado();
        throw new Error('No se pudo validar la sesión guardada. Ingrese con su contraseña.');
      }
    }

    // Si la validación es exitosa: marca autenticado = true, carga el usuario en el store y navega a /app/inicio
    sesion_store.establecer_usuario_autenticado(perfil);
    router.replace('/app/inicio');
  } catch (error) {
    errorMensaje.value = error.mensaje || error.message || 'Error al validar la sesión guardada.';
  } finally {
    cargando.value = false;
  }
}
</script>

<style scoped>
.login-contenedor {
  max-width: 420px;
  margin: 2rem auto;
  display: flex;
  flex-direction: column;
}

.logo-envoltorio {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 76px;
  height: 76px;
  margin: 0 auto 1rem;
  border-radius: 50%;
  background: rgba(var(--ion-color-primary-rgb, 56, 128, 255), 0.12);
}

.icono-logo {
  font-size: 42px;
  color: var(--ion-color-primary, #3880ff);
}

.titulo-login {
  font-size: 1.6rem;
  font-weight: 700;
  text-align: center;
  margin: 0 0 0.4rem;
  color: var(--ion-text-color, #1a1a1a);
}

.subtitulo-login {
  font-size: 0.95rem;
  text-align: center;
  margin: 0 0 1.5rem;
  color: var(--ion-color-medium, #92949c);
}

.formulario-lista {
  margin: 0;
  border-radius: 14px;
}

.error-banner {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 10px 14px;
  background: rgba(var(--ion-color-danger-rgb, 235, 68, 90), 0.12);
  border: 1px solid var(--ion-color-danger, #eb445a);
  border-radius: 10px;
  font-size: 0.9rem;
  line-height: 1.3;
}

.icono-error {
  font-size: 20px;
  flex-shrink: 0;
}

.seccion-biometria {
  text-align: center;
}

.separador {
  position: relative;
  margin: 1.5rem 0 1rem;
  text-align: center;
}

.separador::before {
  content: '';
  position: absolute;
  top: 50%;
  left: 0;
  right: 0;
  height: 1px;
  background: var(--ion-color-step-200, #cccccc);
  z-index: 1;
}

.separador span {
  position: relative;
  z-index: 2;
  background: var(--ion-background-color, #ffffff);
  padding: 0 12px;
  font-size: 0.85rem;
  color: var(--ion-color-medium, #92949c);
}
</style>

<template>
  <comp-page titulo="Mi cuenta">
    <!-- Sección Preferencias -->
    <ion-list inset>
      <ion-list-header>
        <ion-label>Preferencias de la Aplicación</ion-label>
      </ion-list-header>

      <ion-item>
        <ion-icon slot="start" :icon="moonOutline" color="primary"></ion-icon>
        <ion-toggle :checked="esOscuro" @ionChange="cambiarTema">
          Modo oscuro
        </ion-toggle>
      </ion-item>
    </ion-list>

    <!-- Sección Diagnóstico de Conexión -->
    <ion-list inset class="seccion-diagnostico">
      <ion-list-header>
        <ion-label>Diagnóstico de Conexión con Backend</ion-label>
      </ion-list-header>

      <ion-item lines="none">
        <ion-icon slot="start" :icon="serverOutline" color="secondary"></ion-icon>
        <ion-label>
          <h3>URL Destino de la API</h3>
          <p class="url-api">{{ apiUrl }}</p>
        </ion-label>
      </ion-item>

      <ion-item lines="none">
        <ion-icon slot="start" :icon="pulseOutline" :color="colorEstadoIcono"></ion-icon>
        <ion-label>
          <h3>Estado del servicio</h3>
          <p v-if="resultadoPrueba">{{ resultadoPrueba.mensaje }}</p>
          <p v-else>Presione el botón para diagnosticar el endpoint /health</p>
        </ion-label>
        <ion-badge slot="end" :color="colorBadgeEstado">
          {{ textoBadgeEstado }}
        </ion-badge>
      </ion-item>

      <div class="ion-padding">
        <ion-button
          expand="block"
          shape="round"
          :disabled="probando"
          @click="ejecutarDiagnostico"
        >
          <ion-icon
            slot="start"
            :icon="probando ? syncOutline : checkmarkCircleOutline"
            :class="{ 'icono-girando': probando }"
          ></ion-icon>
          {{ probando ? 'Probando conexión...' : 'Probar conexión (/health)' }}
        </ion-button>
      </div>

      <div v-if="resultadoPrueba?.detalles" class="detalles-contenedor ion-padding-horizontal ion-padding-bottom">
        <pre class="codigo-detalles">{{ resultadoPrueba.detalles }}</pre>
      </div>
    </ion-list>
  </comp-page>
</template>

<script setup>
import { ref, computed } from 'vue';
import {
  IonList,
  IonListHeader,
  IonItem,
  IonLabel,
  IonToggle,
  IonBadge,
  IonButton,
  IonIcon,
} from '@ionic/vue';
import {
  moonOutline,
  serverOutline,
  pulseOutline,
  syncOutline,
  checkmarkCircleOutline,
} from 'ionicons/icons';
import CompPage from '@/components/estructura/comp_page.vue';
import { es_tema_oscuro, alternar_tema } from '@/config/tema';
import { obtener_api_url } from '@/config/debug';
import { productos_service } from '@/services/productos_service';

const esOscuro = ref(es_tema_oscuro());
const apiUrl = ref(obtener_api_url());
const probando = ref(false);
const estadoConexion = ref('pendiente'); // 'pendiente' | 'exito' | 'error'
const resultadoPrueba = ref(null);

function cambiarTema() {
  esOscuro.value = alternar_tema();
}

const colorBadgeEstado = computed(() => {
  switch (estadoConexion.value) {
    case 'exito':
      return 'success';
    case 'error':
      return 'danger';
    default:
      return 'medium';
  }
});

const colorEstadoIcono = computed(() => {
  switch (estadoConexion.value) {
    case 'exito':
      return 'success';
    case 'error':
      return 'danger';
    default:
      return 'medium';
  }
});

const textoBadgeEstado = computed(() => {
  switch (estadoConexion.value) {
    case 'exito':
      return 'Online';
    case 'error':
      return 'Offline';
    default:
      return 'Sin probar';
  }
});

async function ejecutarDiagnostico() {
  probando.value = true;
  resultadoPrueba.value = null;
  const inicioTiempo = performance.now();

  try {
    const respuesta = await productos_service.verificar_salud();
    const latencia = Math.round(performance.now() - inicioTiempo);

    estadoConexion.value = 'exito';
    resultadoPrueba.value = {
      mensaje: `Conexión exitosa en ${latencia}ms`,
      detalles: JSON.stringify(respuesta, null, 2),
    };
  } catch (error) {
    estadoConexion.value = 'error';
    resultadoPrueba.value = {
      mensaje: error.message || 'Fallo de conectividad con la API.',
      detalles: error.toString(),
    };
  } finally {
    probando.value = false;
  }
}
</script>

<style scoped>
.seccion-diagnostico {
  margin-top: 1rem;
}

.url-api {
  font-family: monospace;
  font-size: 0.9rem;
  font-weight: 600;
  color: var(--ion-color-secondary, #3dc2ff);
  word-break: break-all;
}

.icono-girando {
  animation: girar 1s linear infinite;
}

@keyframes girar {
  from {
    transform: rotate(0deg);
  }
  to {
    transform: rotate(360deg);
  }
}

.detalles-contenedor {
  margin-top: -0.5rem;
}

.codigo-detalles {
  margin: 0;
  padding: 0.75rem;
  border-radius: 8px;
  background: var(--ion-color-step-100, #f0f0f0);
  font-size: 0.8rem;
  font-family: monospace;
  color: var(--ion-text-color, #1a1a1a);
  white-space: pre-wrap;
  word-break: break-all;
}
</style>

<template>
  <comp-page titulo="Mi cuenta">
    <!-- Advertencia destacada si el usuario no tiene rol -->
    <ion-card v-if="esUsuarioSinRol" color="warning" class="tarjeta-sin-rol">
      <ion-card-header>
        <ion-card-title class="titulo-advertencia">
          <ion-icon :icon="warningOutline" class="icono-alerta"></ion-icon>
          Cuenta sin rol asignado
        </ion-card-title>
      </ion-card-header>
      <ion-card-content>
        Tu cuenta fue creada pero aún no tiene un rol asignado. Un administrador debe habilitarte para poder operar.
      </ion-card-content>
    </ion-card>

    <!-- Sección Perfil de Usuario -->
    <ion-list inset>
      <ion-list-header>
        <ion-label>Datos de la Cuenta</ion-label>
      </ion-list-header>

      <ion-item lines="none">
        <ion-icon slot="start" :icon="personCircleOutline" color="primary"></ion-icon>
        <ion-label>
          <h3>Correo Electrónico</h3>
          <p class="valor-perfil">{{ sesion_store.usuario?.email || 'No disponible' }}</p>
        </ion-label>
      </ion-item>

      <ion-item lines="none">
        <ion-icon slot="start" :icon="shieldCheckmarkOutline" :color="colorRolBadge"></ion-icon>
        <ion-label>
          <h3>Rol en el Sistema</h3>
          <p>{{ sesion_store.rol_nombre || 'Sin rol asignado' }}</p>
        </ion-label>
        <ion-badge slot="end" :color="colorRolBadge">
          {{ sesion_store.rol_codigo || 'SIN ROL' }}
        </ion-badge>
      </ion-item>

      <div class="ion-padding">
        <ion-button
          expand="block"
          fill="outline"
          color="danger"
          shape="round"
          @click="confirmarCerrarSesion"
        >
          <ion-icon slot="start" :icon="logOutOutline"></ion-icon>
          Cerrar sesión
        </ion-button>
      </div>
    </ion-list>

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

      <ion-item>
        <ion-icon slot="start" :icon="phonePortraitOutline" color="secondary"></ion-icon>
        <ion-toggle :checked="tema_store.vibracion" @ionChange="cambiarVibracion($event)">
          Vibración
        </ion-toggle>
      </ion-item>
    </ion-list>
  </comp-page>
</template>

<script setup>
import { ref, computed } from 'vue';
import { useRouter } from 'vue-router';
import {
  IonList,
  IonListHeader,
  IonItem,
  IonLabel,
  IonToggle,
  IonBadge,
  IonButton,
  IonIcon,
  IonCard,
  IonCardHeader,
  IonCardTitle,
  IonCardContent,
  alertController,
} from '@ionic/vue';
import {
  moonOutline,
  phonePortraitOutline,
  personCircleOutline,
  shieldCheckmarkOutline,
  logOutOutline,
  warningOutline,
} from 'ionicons/icons';
import CompPage from '@/components/estructura/comp_page.vue';
import { es_tema_oscuro, alternar_tema } from '@/config/tema';
import { sesion_store } from '@/stores/sesion_store';
import { tema_store } from '@/stores/tema_store';
import { vibrar_toque } from '@/services/vibracion_service';

const router = useRouter();

const esOscuro = ref(es_tema_oscuro());

const esUsuarioSinRol = computed(() => {
  return sesion_store.autenticado && !sesion_store.rol_codigo;
});

const colorRolBadge = computed(() => {
  switch (sesion_store.rol_codigo) {
    case 'ADMIN':
      return 'success';
    case 'VETERINARIO':
      return 'primary';
    default:
      return 'warning';
  }
});

async function cambiarTema() {
  esOscuro.value = alternar_tema();
  await vibrar_toque();
}

async function cambiarVibracion(event) {
  const nuevoValor = event?.detail !== undefined ? event.detail.checked : !tema_store.vibracion;
  tema_store.establecer_vibracion(nuevoValor);
  await vibrar_toque(true);
}

async function confirmarCerrarSesion() {
  const alerta = await alertController.create({
    header: 'Cerrar sesión',
    message: '¿Estás seguro de que deseas cerrar tu sesión?',
    buttons: [
      {
        text: 'Cancelar',
        role: 'cancel',
      },
      {
        text: 'Cerrar sesión',
        role: 'destructive',
        handler: async () => {
          await sesion_store.logout();
          router.replace('/login');
        },
      },
    ],
  });

  await alerta.present();
}
</script>

<style scoped>
.tarjeta-sin-rol {
  border-radius: 14px;
  margin: 1rem;
}

.titulo-advertencia {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 1.15rem;
  font-weight: 700;
}

.icono-alerta {
  font-size: 24px;
}

.valor-perfil {
  font-weight: 600;
  color: var(--ion-text-color, #1a1a1a);
}
</style>

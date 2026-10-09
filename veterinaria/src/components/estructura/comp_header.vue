<template>
  <ion-header>
    <ion-toolbar>
      <ion-buttons slot="start">
        <ion-menu-button></ion-menu-button>
      </ion-buttons>

      <ion-title>{{ titulo }}</ion-title>

      <ion-buttons slot="end">
        <slot name="acciones" />

        <!-- Botón de Notificaciones con Badge de no leídas condicionado por sesión activa -->
        <ion-button
          v-if="sesion_store.autenticado"
          class="boton-campana"
          @click="irANotificaciones"
          title="Notificaciones"
        >
          <ion-icon slot="icon-only" :icon="notificationsOutline"></ion-icon>
          <ion-badge
            v-if="notificaciones_store.no_leidas > 0"
            color="danger"
            class="badge-notificaciones"
          >
            {{ notificaciones_store.no_leidas > 99 ? '99+' : notificaciones_store.no_leidas }}
          </ion-badge>
        </ion-button>
      </ion-buttons>
    </ion-toolbar>
  </ion-header>
</template>

<script setup>
import {
  IonHeader,
  IonToolbar,
  IonTitle,
  IonButtons,
  IonMenuButton,
  IonButton,
  IonIcon,
  IonBadge,
} from '@ionic/vue';
import { notificationsOutline } from 'ionicons/icons';
import { useRouter } from 'vue-router';
import { sesion_store } from '@/stores/sesion_store';
import { notificaciones_store } from '@/stores/notificaciones_store';

defineProps({
  titulo: { type: String, required: true },
});

const router = useRouter();

function irANotificaciones() {
  router.push('/app/notificaciones');
}
</script>

<style scoped>
.boton-campana {
  position: relative;
}

.badge-notificaciones {
  position: absolute;
  top: 4px;
  right: 2px;
  font-size: 10px;
  font-weight: 700;
  padding: 2px 5px;
  border-radius: 10px;
  box-shadow: 0 1px 4px rgba(0, 0, 0, 0.25);
  pointer-events: none;
}
</style>

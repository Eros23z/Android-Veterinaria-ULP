<template>
  <ion-app>
    <ion-router-outlet />
  </ion-app>
</template>

<script setup lang="ts">
import { onMounted } from 'vue';
import { IonApp, IonRouterOutlet } from '@ionic/vue';
import { notificaciones_nativas_service } from '@/services/notificaciones_nativas_service';
import { inicializar_red } from '@/stores/offline_store';

onMounted(() => {
  inicializar_red().catch((e) => console.warn('Error al inicializar escucha de red:', e));
  notificaciones_nativas_service.preparar_push().catch(() => {});
});
</script>

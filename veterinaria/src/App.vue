<template>
  <ion-app>
    <ion-router-outlet />
  </ion-app>
</template>

<script setup lang="ts">
import { onMounted } from 'vue';
import { IonApp, IonRouterOutlet } from '@ionic/vue';
import { notificaciones_nativas_service } from '@/services/notificaciones_nativas_service';

onMounted(() => {
  notificaciones_nativas_service.preparar_push().catch(() => {});
});
</script>

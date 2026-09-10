<template>
  <ion-menu :content-id="content_id">
    <ion-header>
      <ion-toolbar>
        <ion-title>Veterinaria San Roque</ion-title>
      </ion-toolbar>
    </ion-header>
    <ion-content>
      <ion-list>
        <ion-list-header>Operación</ion-list-header>
        <ion-menu-toggle
          :auto-hide="false"
          v-for="item in operacion"
          :key="item.id"
        >
          <ion-item
            :router-link="item.ruta"
            router-direction="root"
            lines="none"
          >
            <ion-icon slot="start" :icon="item.icono"></ion-icon>
            <ion-label>{{ item.titulo }}</ion-label>
          </ion-item>
        </ion-menu-toggle>

        <ion-list-header>Configuración</ion-list-header>
        <ion-menu-toggle
          :auto-hide="false"
          v-for="item in configuracion"
          :key="item.id"
        >
          <ion-item
            :router-link="item.ruta"
            router-direction="root"
            lines="none"
          >
            <ion-icon slot="start" :icon="item.icono"></ion-icon>
            <ion-label>{{ item.titulo }}</ion-label>
          </ion-item>
        </ion-menu-toggle>
      </ion-list>
    </ion-content>
  </ion-menu>
</template>

<script setup>
import { computed } from "vue";
import {
  IonMenu,
  IonHeader,
  IonToolbar,
  IonTitle,
  IonContent,
  IonList,
  IonListHeader,
  IonItem,
  IonIcon,
  IonLabel,
  IonMenuToggle,
} from "@ionic/vue";
import { navegacion } from "@/config/navegacion";

defineProps({
  content_id: { type: String, required: true },
});

const operacion = computed(() =>
  navegacion
    .filter((item) => item.grupo_menu === "operacion")
    .sort((a, b) => a.orden - b.orden),
);
const configuracion = computed(() =>
  navegacion
    .filter((item) => item.grupo_menu === "configuracion")
    .sort((a, b) => a.orden - b.orden),
);
</script>

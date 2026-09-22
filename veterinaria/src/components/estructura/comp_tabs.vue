<template>
  <ion-tab-bar slot="bottom">
    <ion-tab-button
      v-for="item in tabs"
      :key="item.id"
      :tab="item.id"
      :href="item.ruta"
    >
      <ion-icon :icon="item.icono" />
      <ion-label>{{ item.titulo }}</ion-label>
    </ion-tab-button>
  </ion-tab-bar>
</template>

<script setup>
import { computed } from "vue";
import { IonTabBar, IonTabButton, IonIcon, IonLabel } from "@ionic/vue";
import { navegacion } from "@/config/navegacion";
import { sesion_store } from "@/stores/sesion_store";

const tabs = computed(() =>
  [...navegacion]
    .filter(
      (item) =>
        item.tab_roles &&
        item.tab_roles.length > 0 &&
        sesion_store.tiene_rol(item.tab_roles),
    )
    .sort((a, b) => a.orden - b.orden),
);
</script>

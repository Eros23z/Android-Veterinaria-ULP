<template>
  <ion-page>
    <comp-header :titulo="titulo">
      <template #acciones>
        <slot name="acciones" />
      </template>
    </comp-header>

    <ion-content :fullscreen="true">
      <ion-refresher
        v-if="mostrar_actualizar"
        slot="fixed"
        @ionRefresh="ejecutarActualizar($event)"
      >
        <ion-refresher-content></ion-refresher-content>
      </ion-refresher>
      <slot />
    </ion-content>
  </ion-page>
</template>

<script setup>
import {
  IonPage,
  IonContent,
  IonRefresher,
  IonRefresherContent,
} from "@ionic/vue";
import compHeader from "./comp_header.vue";

defineProps({
  titulo: { type: String, required: true },
  mostrar_actualizar: { type: Boolean, default: false },
});

const emit = defineEmits(["actualizar"]);

function ejecutarActualizar(event) {
  emit("actualizar", event);
}
</script>

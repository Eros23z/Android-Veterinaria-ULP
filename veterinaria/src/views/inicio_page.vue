<template>
  <comp-page titulo="Inicio">
    <div class="ion-padding">
      <h1>Veterinaria</h1>
      <p>Turnos, vacunas y controles en un solo lugar</p>
    </div>

    <ion-card v-for="item in servicios" :key="item.titulo">
      <ion-card-header>
        <ion-card-subtitle>{{ item.dias }}</ion-card-subtitle>
        <ion-card-title>{{ item.titulo }}</ion-card-title>
      </ion-card-header>
      <ion-card-content>
        {{ item.detalle }}
      </ion-card-content>
    </ion-card>
  </comp-page>
</template>

<script setup>
import { ref } from "vue";
import {
  IonCard,
  IonCardHeader,
  IonCardSubtitle,
  IonCardTitle,
  IonCardContent,
} from "@ionic/vue";
import CompPage from "@/components/estructura/comp_page.vue";

const servicios = ref([
  {
    titulo: "Consulta general",
    dias: "Lunes a viernes",
    detalle: "Control clínico general y plan de vacunación.",
  },
  {
    titulo: "Peluquería canina",
    dias: "Martes y jueves",
    detalle: "Baño, corte higiénico y desparasitación externa.",
  },
  {
    titulo: "Guardia y urgencias",
    dias: "Sábados y domingos",
    detalle: "Atención de emergencias 24 hs sin turno previo.",
  },
]);
</script>

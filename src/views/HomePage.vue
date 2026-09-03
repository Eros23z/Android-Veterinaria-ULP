<template>
  <ion-page>
    <ion-header :translucent="true">
      <ion-toolbar>
        <ion-title>Eros Zamora</ion-title>
        <ion-buttons slot="end">
          <ion-button @click="toggleTema">
            <ion-icon slot="icon-only" :icon="moonOutline"></ion-icon>
          </ion-button>
        </ion-buttons>
      </ion-toolbar>
    </ion-header>

    <ion-content :fullscreen="true">
      <div class="ion-padding">
        <h1>Veterinaria</h1>
        <p>Turnos, vacunas y controles en un solo lugar</p>
      </div>

      <!-- Tarjetas -->
      <ion-card v-for="item in servicios" :key="item.titulo">
        <ion-card-header>
          <ion-card-subtitle>{{ item.dias }}</ion-card-subtitle>
          <ion-card-title>{{ item.titulo }}</ion-card-title>
        </ion-card-header>
        <ion-card-content>
          {{ item.detalle }}
        </ion-card-content>
      </ion-card>
    </ion-content>
  </ion-page>
</template>

<script setup>
import { ref, onMounted } from "vue";
import {
  IonPage,
  IonHeader,
  IonToolbar,
  IonTitle,
  IonButtons,
  IonButton,
  IonIcon,
  IonContent,
  IonCard,
  IonCardHeader,
  IonCardSubtitle,
  IonCardTitle,
  IonCardContent,
} from "@ionic/vue";
import { moonOutline } from "ionicons/icons";

// Datos
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

// Manejo del tema
const toggleTema = () => {
  const html = document.documentElement;
  const esOscuro = html.classList.toggle("ion-palette-dark");
  localStorage.setItem("mi_app_tema", esOscuro ? "oscuro" : "claro");
};

onMounted(() => {
  const temaGuardado = localStorage.getItem("mi_app_tema");
  if (temaGuardado === "oscuro") {
    document.documentElement.classList.add("ion-palette-dark");
  }
});
</script>

<template>
  <comp-page titulo="Mi cuenta">
    <ion-list>
      <ion-item>
        <ion-toggle :checked="esOscuro" @ionChange="cambiarTema">
          Modo oscuro
        </ion-toggle>
      </ion-item>
    </ion-list>
  </comp-page>
</template>

<script setup>
import { ref } from "vue";
import { IonList, IonItem, IonToggle } from "@ionic/vue";
import compPage from "@/components/estructura/comp_page.vue";
import { es_tema_oscuro, alternar_tema } from "@/config/tema";

const esOscuro = ref(es_tema_oscuro());

function cambiarTema() {
  esOscuro.value = alternar_tema();
}
</script>

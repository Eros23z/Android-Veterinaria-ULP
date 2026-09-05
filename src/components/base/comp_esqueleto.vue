<template>
  <ion-list>
    <ion-item v-for="n in 5" :key="n">
      <ion-label>
        <h3>
          <ion-skeleton-text
            :animated="true"
            style="width: 60%"
          ></ion-skeleton-text>
        </h3>
        <p>
          <ion-skeleton-text
            :animated="true"
            style="width: 40%"
          ></ion-skeleton-text>
        </p>
        <p>
          <ion-skeleton-text
            :animated="true"
            style="width: 25%"
          ></ion-skeleton-text>
        </p>
      </ion-label>
    </ion-item>
  </ion-list>
</template>

<script setup>
import { IonList, IonItem, IonLabel, IonSkeletonText } from "@ionic/vue";
</script>

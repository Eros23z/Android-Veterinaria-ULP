<template>
  <comp-page titulo="Pedidos" :mostrar_actualizar="true" @actualizar="cargar">
    <template #acciones>
      <ion-button @click="ejecutarEscaneoQr" title="Escanear comprobante">
        <ion-icon slot="icon-only" :icon="qrCodeOutline"></ion-icon>
      </ion-button>
    </template>

    <!-- Barra de búsqueda y acceso directo a escaneo de QR -->
    <div class="ion-padding-horizontal ion-padding-top seccion-acciones-superiores">
      <ion-button
        expand="block"
        shape="round"
        fill="outline"
        color="secondary"
        @click="ejecutarEscaneoQr"
      >
        <ion-icon slot="start" :icon="qrCodeOutline"></ion-icon>
        Escanear Comprobante (QR)
      </ion-button>
    </div>

    <comp-buscador
      placeholder="Buscar pedidos o cliente..."
      @buscar="onBuscar"
    />

    <comp-esqueleto v-if="pedidos_store.cargando && pedidos_store.pedidos.length === 0" />

    <div v-else-if="pedidos_store.pedidos.length === 0" class="ion-padding ion-text-center">
      <p>No hay pedidos registrados.</p>
    </div>

    <comp-lista
      v-else
      :total="pedidos_store.paginacion.total"
      :cantidad_mostrada="pedidos_store.pedidos.length"
      :hay_mas="pedidos_store.paginacion.hay_mas"
      :cargando="pedidos_store.cargando"
      @ver_mas="cargarMas"
    >
      <template #lista>
        <ion-list>
          <div
            v-for="p in pedidos_store.pedidos"
            :key="p.id"
            class="tarjeta-pedido-envoltorio"
            :class="{ 'pedido-seleccionado': pedidoExpandidoId === p.id }"
          >
            <ion-item button :detail="false" @click="alternarExpansion(p.id)">
              <ion-icon
                slot="start"
                :icon="receiptOutline"
                color="primary"
                class="icono-pedido"
              ></ion-icon>

              <ion-label>
                <h2>
                  <strong>{{ formatearCodigo(p.id) }}</strong>
                  <span class="separador-nombre"> · </span>
                  <span>{{ p.cliente?.nombre || "Cliente no especificado" }}</span>
                </h2>
                <p>Fecha: {{ new Date(p.fecha_pedido).toLocaleDateString() }}</p>
                <p class="precio-destacado">${{ Number(p.total).toLocaleString('es-AR', { minimumFractionDigits: 2 }) }}</p>
              </ion-label>

              <div slot="end" class="bloque-estado-acciones">
                <ion-badge :color="obtenerColorEstado(p.estado)">
                  {{ formatEstado(p.estado) }}
                </ion-badge>
                <ion-icon
                  :icon="pedidoExpandidoId === p.id ? chevronUpOutline : chevronDownOutline"
                  color="medium"
                  class="icono-chevron"
                ></ion-icon>
              </div>
            </ion-item>

            <!-- Sección Expandida con detalles, Código QR y Compartir PDF -->
            <div v-if="pedidoExpandidoId === p.id" class="seccion-expandida ion-padding">
              <!-- Información de items del pedido -->
              <div v-if="p.items && p.items.length > 0" class="items-pedido-tabla">
                <h4 class="subtitulo-seccion">Ítems del pedido:</h4>
                <div v-for="item in p.items" :key="item.id" class="fila-item">
                  <span>{{ item.cantidad }}x {{ item.producto?.nombre || `Producto #${item.producto_id}` }}</span>
                  <span class="subtotal-item">${{ Number(item.subtotal).toLocaleString('es-AR', { minimumFractionDigits: 2 }) }}</span>
                </div>
              </div>

              <div v-if="p.notas" class="notas-pedido ion-margin-top">
                <p><em>Observaciones: {{ p.notas }}</em></p>
              </div>

              <!-- Imagen del Código QR del pedido -->
              <div class="contenedor-qr ion-margin-top ion-text-center">
                <img
                  :src="resolverQrUrl(p.id)"
                  alt="Código QR del pedido"
                  class="imagen-qr"
                />
                <p class="texto-codigo-qr">{{ formatearCodigo(p.id) }}</p>
                <p class="instruccion-qr">Código verificable para escaneo clínico</p>
              </div>

              <!-- Botón Compartir Comprobante (solo pedidos confirmados o superiores) -->
              <div class="botones-accion ion-margin-top">
                <ion-button
                  v-if="puedeImprimirComprobante(p.estado)"
                  expand="block"
                  shape="round"
                  color="primary"
                  :disabled="compartiendoId === p.id"
                  @click.stop="ejecutarCompartirComprobante(p)"
                >
                  <ion-spinner v-if="compartiendoId === p.id" name="crescent" slot="start"></ion-spinner>
                  <ion-icon v-else slot="start" :icon="shareSocialOutline"></ion-icon>
                  {{ compartiendoId === p.id ? 'Descargando comprobante...' : 'Compartir comprobante' }}
                </ion-button>

                <p v-else class="aviso-borrador ion-text-center">
                  El comprobante solo está disponible para pedidos confirmados o superiores.
                </p>
              </div>
            </div>
          </div>
        </ion-list>
      </template>
    </comp-lista>
  </comp-page>
</template>

<script setup>
import { ref, onMounted } from "vue";
import {
  IonList,
  IonItem,
  IonLabel,
  IonBadge,
  IonButton,
  IonIcon,
  IonSpinner,
  alertController,
} from "@ionic/vue";
import {
  qrCodeOutline,
  receiptOutline,
  shareSocialOutline,
  chevronDownOutline,
  chevronUpOutline,
} from "ionicons/icons";
import compPage from "@/components/estructura/comp_page.vue";
import compEsqueleto from "@/components/base/comp_esqueleto.vue";
import compBuscador from "@/components/base/comp_buscador.vue";
import compLista from "@/components/base/comp_lista.vue";
import { pedidos_store } from "@/stores/pedidos_store";
import { qr_service } from "@/services/qr_service";
import { compartir_service } from "@/services/compartir_service";
import { ajax_binario } from "@/services/ajax_service";
import { vibracion_service } from "@/services/vibracion_service";
import { obtener_api_url } from "@/config/debug";

const terminoBusqueda = ref("");
const pedidoExpandidoId = ref(null);
const compartiendoId = ref(null);

function formatearCodigo(id) {
  if (!id) return "";
  return `PED-${String(id).padStart(4, "0")}`;
}

function puedeImprimirComprobante(estado) {
  return estado && estado !== "borrador";
}

function resolverQrUrl(id) {
  return `${obtener_api_url()}/api/pedidos/${id}/qr`;
}

async function alternarExpansion(id) {
  pedidoExpandidoId.value = pedidoExpandidoId.value === id ? null : id;
  await vibracion_service.vibrar_toque();
}

function formatEstado(estado) {
  if (!estado) return "";
  return estado.replace(/_/g, " ").replace(/\b\w/g, (l) => l.toUpperCase());
}

function obtenerColorEstado(estado) {
  switch (estado) {
    case "borrador":
      return "light";
    case "confirmado":
      return "primary";
    case "en_preparacion":
      return "warning";
    case "listo":
      return "success";
    case "entregado":
      return "tertiary";
    case "cerrado":
      return "dark";
    case "cancelado":
      return "danger";
    default:
      return "medium";
  }
}

async function cargar(event = null) {
  pedidos_store.reiniciar_paginacion();
  await pedidos_store.cargar_pedidos({ busqueda: terminoBusqueda.value });
  if (event?.target?.complete) {
    event.target.complete();
  }
}

async function cargarMas() {
  await pedidos_store.cargar_mas_pedidos();
}

function onBuscar(termino) {
  terminoBusqueda.value = termino;
  cargar();
}

function extraerIdDesdeTextoQr(texto) {
  if (!texto) return null;
  const match = texto.match(/PED-(\d+)/i);
  if (match) {
    return parseInt(match[1], 10);
  }
  const numero = parseInt(texto.replace(/\D/g, ""), 10);
  return isNaN(numero) ? null : numero;
}

async function ejecutarEscaneoQr() {
  try {
    const textoQr = await qr_service.escanear_qr();
    if (!textoQr) return;

    await vibracion_service.vibrar_toque();

    const id = extraerIdDesdeTextoQr(textoQr);
    if (!id) {
      const alerta = await alertController.create({
        header: "Código no reconocido",
        message: `El código escaneado ("${textoQr}") no coincide con el formato esperado de comprobante (PED-0000).`,
        buttons: ["Entendido"],
      });
      await alerta.present();
      return;
    }

    // Filtrar y expandir inmediatamente el pedido en la vista
    terminoBusqueda.value = `PED-${String(id).padStart(4, "0")}`;
    pedidoExpandidoId.value = id;

    // Cargar con búsqueda del ID
    await pedidos_store.cargar_pedidos({ busqueda: id.toString() });

    const encontrado = pedidos_store.pedidos.find((p) => p.id === id);
    if (encontrado) {
      pedidoExpandidoId.value = encontrado.id;
    } else {
      // Recargar sin filtro para localizarlo
      await pedidos_store.cargar_pedidos({ busqueda: "" });
      const localizado = pedidos_store.pedidos.find((p) => p.id === id);
      if (localizado) {
        pedidoExpandidoId.value = localizado.id;
      }
    }
  } catch (error) {
    await vibracion_service.vibrar_error();
    console.error("Error al escanear QR:", error);
    const alerta = await alertController.create({
      header: "Error de Escaneo",
      message: error.message || "No se pudo completar el escaneo del código.",
      buttons: ["Cerrar"],
    });
    await alerta.present();
  }
}

async function ejecutarCompartirComprobante(pedido) {
  if (!puedeImprimirComprobante(pedido.estado)) return;

  compartiendoId.value = pedido.id;
  try {
    await vibracion_service.vibrar_toque();
    const nombreArchivo = `${formatearCodigo(pedido.id)}.pdf`;
    const blobPdf = await ajax_binario(`/api/pedidos/${pedido.id}/comprobante`);

    await compartir_service.compartir_archivo({
      nombre_archivo: nombreArchivo,
      blob: blobPdf,
      titulo: `Comprobante ${formatearCodigo(pedido.id)}`,
      texto: `Comprobante de pedido para ${pedido.cliente?.nombre || "Cliente"} - Veterinaria San Roque`,
    });
  } catch (error) {
    await vibracion_service.vibrar_error();
    console.error("Error al compartir comprobante:", error);
    const alerta = await alertController.create({
      header: "No se pudo compartir",
      message: error.mensaje || error.message || "Error al obtener el comprobante del servidor.",
      buttons: ["Aceptar"],
    });
    await alerta.present();
  } finally {
    compartiendoId.value = null;
  }
}

onMounted(() => cargar());
</script>

<style scoped>
.seccion-acciones-superiores {
  margin-bottom: -0.5rem;
}

.tarjeta-pedido-envoltorio {
  border-bottom: 1px solid var(--ion-color-step-150, #e0e0e0);
  transition: background-color 0.2s ease;
}

.pedido-seleccionado {
  background: rgba(var(--ion-color-primary-rgb, 56, 128, 255), 0.05);
}

.icono-pedido {
  font-size: 26px;
}

.separador-nombre {
  color: var(--ion-color-medium, #92949c);
}

.precio-destacado {
  font-weight: 700;
  color: var(--ion-color-success, #2dd36f);
}

.bloque-estado-acciones {
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  gap: 6px;
}

.icono-chevron {
  font-size: 16px;
}

.seccion-expandida {
  background: var(--ion-color-step-50, #f8f9fa);
  border-top: 1px dashed var(--ion-color-step-200, #cccccc);
  border-radius: 0 0 12px 12px;
}

.subtitulo-seccion {
  margin: 0 0 8px;
  font-size: 0.95rem;
  font-weight: 600;
  color: var(--ion-color-step-800, #333333);
}

.fila-item {
  display: flex;
  justify-content: space-between;
  padding: 4px 0;
  font-size: 0.9rem;
  border-bottom: 1px solid var(--ion-color-step-100, #eeeeee);
}

.subtotal-item {
  font-weight: 600;
}

.contenedor-qr {
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 12px;
  background: #ffffff;
  border-radius: 12px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);
}

.imagen-qr {
  width: 140px;
  height: 140px;
  object-fit: contain;
}

.texto-codigo-qr {
  font-weight: 700;
  font-size: 1.1rem;
  letter-spacing: 1px;
  margin: 8px 0 2px;
  color: var(--ion-color-primary, #3880ff);
}

.instruccion-qr {
  font-size: 0.8rem;
  color: var(--ion-color-medium, #92949c);
  margin: 0;
}

.aviso-borrador {
  font-size: 0.85rem;
  font-style: italic;
  color: var(--ion-color-medium, #92949c);
  margin: 8px 0 0;
}
</style>

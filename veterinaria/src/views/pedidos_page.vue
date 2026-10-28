<template>
  <comp-page titulo="Pedidos" :mostrar_actualizar="true" @actualizar="cargar">
    <template #acciones>
      <ion-button v-if="!offline_store.sin_conexion" @click="abrirNuevoPedido" title="Nuevo Pedido">
        <ion-icon slot="icon-only" :icon="addOutline"></ion-icon>
      </ion-button>
      <ion-button v-if="!offline_store.sin_conexion" @click="ejecutarEscaneoQr" title="Escanear comprobante">
        <ion-icon slot="icon-only" :icon="qrCodeOutline"></ion-icon>
      </ion-button>
    </template>

    <!-- Banner informativo de Modo Offline con comp-aviso -->
    <div v-if="offline_store.sin_conexion" class="ion-padding-horizontal ion-padding-top">
      <comp-aviso
        tono="advertencia"
        titulo="Modo sin conexión"
        mensaje="Estás viendo los datos guardados en tu dispositivo. Podés cambiar el estado de un pedido; se enviará automáticamente cuando vuelva la red."
      />
    </div>

    <!-- Barra de búsqueda y acceso directo a escaneo de QR (ocultos sin red) -->
    <div v-if="!offline_store.sin_conexion" class="ion-padding-horizontal ion-padding-top">
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
      v-if="!offline_store.sin_conexion"
      placeholder="Buscar pedidos o cliente..."
      @buscar="onBuscar"
    />

    <!-- Barra de indicadores de resumen por estado -->
    <comp-resumen-indicadores
      :indicadores="indicadoresFiltro"
      :seleccionado="estadoSeleccionado"
      @seleccionar="onSeleccionarEstado"
    />

    <!-- Estado de carga con esqueleto estilizado -->
    <comp-esqueleto v-if="pedidos_store.cargando && pedidos_store.pedidos.length === 0" :cantidad="4" />

    <!-- Estado vacío contextual -->
    <comp-estado-vacio
      v-else-if="pedidos_store.pedidos.length === 0"
      :tipo="tipoEstadoVacio"
      :texto-boton="textoBotonVacio"
      @accion="onAccionVacio"
    />

    <!-- Listado de tarjetas de pedidos -->
    <comp-lista
      v-else
      :total="pedidos_store.paginacion.total"
      :cantidad_mostrada="pedidos_store.pedidos.length"
      :hay_mas="pedidos_store.paginacion.hay_mas"
      :cargando="pedidos_store.cargando"
      @ver_mas="cargarMas"
    >
      <template #lista>
        <div class="lista-tarjetas-pedidos ion-padding-horizontal">
          <div
            v-for="p in pedidos_store.pedidos"
            :key="p.id"
            class="vet-card tarjeta-pedido"
            :class="{ 'tarjeta-expandida': pedidoExpandidoId === p.id }"
          >
            <!-- Fila principal de la tarjeta -->
            <div class="tarjeta-cabecera" @click="alternarExpansion(p.id)">
              <div class="tarjeta-icono-envoltorio">
                <ion-icon :icon="receiptOutline" class="tarjeta-icono-pedido"></ion-icon>
              </div>

              <div class="tarjeta-info">
                <div class="tarjeta-linea-superior">
                  <h3 class="codigo-pedido">{{ formatearCodigo(p.id) }}</h3>
                  <span class="separador-punto">·</span>
                  <span class="nombre-cliente">{{ p.cliente?.nombre || 'Cliente sin nombre' }}</span>
                </div>
                <p class="fecha-pedido">Fecha: {{ new Date(p.fecha_pedido).toLocaleDateString() }}</p>
                <p class="precio-pedido">${{ Number(p.total).toLocaleString('es-AR', { minimumFractionDigits: 2 }) }}</p>
              </div>

              <div class="tarjeta-lateral">
                <div class="insignias-contenedor">
                  <ion-badge v-if="p.sin_enviar" color="warning" class="badge-sin-enviar">
                    Sin enviar
                  </ion-badge>
                  <span
                    class="vet-badge-pildora"
                    :style="{
                      background: obtener_config_estado(p.estado).fondo_translúcido,
                      color: obtener_config_estado(p.estado).color_texto
                    }"
                  >
                    <ion-icon :icon="obtener_config_estado(p.estado).icono"></ion-icon>
                    {{ obtener_config_estado(p.estado).texto }}
                  </span>
                </div>
                <ion-icon
                  :icon="pedidoExpandidoId === p.id ? chevronUpOutline : chevronDownOutline"
                  class="icono-chevron"
                ></ion-icon>
              </div>
            </div>

            <!-- Ficha Deslizable de Detalle (Hoja Deslizable) -->
            <div v-if="pedidoExpandidoId === p.id" class="ficha-deslizable">
              <!-- Cabecera sticky de la ficha -->
              <div
                class="ficha-cabecera-sticky"
                :style="{
                  background: obtener_config_estado(p.estado).fondo_translúcido,
                  borderBottomColor: obtener_config_estado(p.estado).color_texto
                }"
              >
                <div class="ficha-cabecera-info">
                  <span class="ficha-subtitulo">Detalle del pedido</span>
                  <strong class="ficha-titulo">{{ formatearCodigo(p.id) }}</strong>
                </div>
                <span
                  class="vet-badge-pildora"
                  :style="{
                    background: '#ffffff',
                    color: obtener_config_estado(p.estado).color_texto,
                    boxShadow: '0 2px 6px rgba(0,0,0,0.08)'
                  }"
                >
                  <ion-icon :icon="obtener_config_estado(p.estado).icono"></ion-icon>
                  {{ obtener_config_estado(p.estado).texto }}
                </span>
              </div>

              <!-- Cuerpo de la ficha deslizable -->
              <div class="ficha-cuerpo">
                <!-- Tabla de Ítems -->
                <div v-if="p.items && p.items.length > 0" class="seccion-items">
                  <h4 class="items-titulo">Ítems del pedido</h4>
                  <div v-for="item in p.items" :key="item.id" class="item-fila">
                    <span class="item-desc">{{ item.cantidad }}x {{ item.producto?.nombre || `Producto #${item.producto_id}` }}</span>
                    <strong class="item-subtotal">${{ Number(item.subtotal).toLocaleString('es-AR', { minimumFractionDigits: 2 }) }}</strong>
                  </div>
                </div>

                <div v-if="p.notas" class="seccion-notas">
                  <p class="notas-texto"><em>Observaciones: {{ p.notas }}</em></p>
                </div>

                <!-- Imagen del Código QR (Oculto sin red) -->
                <div v-if="!offline_store.sin_conexion" class="contenedor-qr">
                  <img
                    :src="resolverQrUrl(p.id)"
                    alt="Código QR del pedido"
                    class="imagen-qr"
                  />
                  <span class="texto-qr">{{ formatearCodigo(p.id) }}</span>
                  <span class="instruccion-qr">Código verificable para escaneo clínico</span>
                </div>

                <!-- Botones de Acción -->
                <div class="ficha-acciones">
                  <template v-if="!offline_store.sin_conexion">
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

                    <p v-else class="aviso-borrador">
                      El comprobante solo está disponible para pedidos confirmados o superiores.
                    </p>
                  </template>

                  <!-- Botón Cambiar Estado (ADMIN o VETERINARIO) -->
                  <ion-button
                    v-if="puedeCambiarEstado && tieneTransiciones(p.estado)"
                    expand="block"
                    shape="round"
                    fill="outline"
                    color="secondary"
                    :disabled="cambiandoEstadoId === p.id"
                    @click.stop="abrirSelectorEstado(p)"
                  >
                    <ion-spinner v-if="cambiandoEstadoId === p.id" name="crescent" slot="start"></ion-spinner>
                    <ion-icon v-else slot="start" :icon="swapHorizontalOutline"></ion-icon>
                    {{ cambiandoEstadoId === p.id ? 'Actualizando estado...' : 'Cambiar estado' }}
                  </ion-button>
                </div>
              </div>
            </div>
          </div>
        </div>
      </template>
    </comp-lista>

    <!-- Botón flotante para nuevo pedido (oculto sin red) -->
    <ion-fab v-if="!offline_store.sin_conexion" slot="fixed" vertical="bottom" horizontal="end">
      <ion-fab-button @click="abrirNuevoPedido" title="Crear pedido">
        <ion-icon :icon="addOutline"></ion-icon>
      </ion-fab-button>
    </ion-fab>

    <!-- Modal de creación de pedido -->
    <modal-pedido
      :is_open="mostrarModalPedido"
      @cerrar="mostrarModalPedido = false"
      @creado="onPedidoCreado"
    />
  </comp-page>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue';
import {
  IonBadge,
  IonButton,
  IonIcon,
  IonSpinner,
  IonFab,
  IonFabButton,
  alertController,
  actionSheetController,
} from '@ionic/vue';
import {
  addOutline,
  qrCodeOutline,
  receiptOutline,
  shareSocialOutline,
  chevronDownOutline,
  chevronUpOutline,
  swapHorizontalOutline,
  checkmarkCircleOutline,
  timeOutline,
  checkmarkDoneOutline,
  checkmarkDoneCircleOutline,
  closeCircleOutline,
  closeOutline,
} from 'ionicons/icons';
import compPage from '@/components/estructura/comp_page.vue';
import compEsqueleto from '@/components/base/comp_esqueleto.vue';
import compBuscador from '@/components/base/comp_buscador.vue';
import compLista from '@/components/base/comp_lista.vue';
import compAviso from '@/components/base/comp_aviso.vue';
import compEstadoVacio from '@/components/base/comp_estado_vacio.vue';
import compResumenIndicadores from '@/components/base/comp_resumen_indicadores.vue';
import modalPedido from '@/components/dominio/modal_pedido.vue';
import { pedidos_store } from '@/stores/pedidos_store';
import { offline_store } from '@/stores/offline_store';
import { sesion_store } from '@/stores/sesion_store';
import { notificaciones_store } from '@/stores/notificaciones_store';
import { qr_service } from '@/services/qr_service';
import { compartir_service } from '@/services/compartir_service';
import { ajax_binario } from '@/services/ajax_service';
import { vibracion_service } from '@/services/vibracion_service';
import { notificaciones_nativas_service } from '@/services/notificaciones_nativas_service';
import { obtener_api_url } from '@/config/debug';
import { obtener_config_estado, normalizar_estado } from '@/utils/estados';

const terminoBusqueda = ref('');
const estadoSeleccionado = ref(null);
const pedidoExpandidoId = ref(null);
const compartiendoId = ref(null);
const cambiandoEstadoId = ref(null);
const mostrarModalPedido = ref(false);

const puedeCambiarEstado = computed(() => {
  const rol = sesion_store.rol_codigo ? String(sesion_store.rol_codigo).toUpperCase() : '';
  return rol === 'ADMIN' || rol === 'VETERINARIO';
});

// Barra de indicadores de resumen por estado
const indicadoresFiltro = computed(() => {
  const pedidos = pedidos_store.pedidos || [];
  const resumen = pedidos_store.resumen || {};
  const tieneResumen = resumen && Object.keys(resumen).length > 0;
  const conteo = {};
  let totalTodos = 0;

  if (tieneResumen) {
    for (const [k, v] of Object.entries(resumen)) {
      if (k !== 'todos') {
        conteo[k] = Number(v) || 0;
        totalTodos += conteo[k];
      }
    }
    if (resumen.todos !== undefined && resumen.todos !== null) {
      totalTodos = Number(resumen.todos);
    }
  } else {
    for (const p of pedidos) {
      const e = normalizar_estado(p.estado);
      conteo[e] = (conteo[e] || 0) + 1;
    }
    totalTodos = pedidos_store.paginacion.total || pedidos.length;
  }

  return [
    { id: null, etiqueta: 'Todos', cantidad: totalTodos, color: 'medium' },
    { id: 'confirmado', etiqueta: 'Confirmados', cantidad: conteo['confirmado'] || 0, color: 'primary' },
    { id: 'en_preparacion', etiqueta: 'En preparación', cantidad: conteo['en_preparacion'] || 0, color: 'warning' },
    { id: 'listo', etiqueta: 'Listos', cantidad: conteo['listo'] || 0, color: 'success' },
    { id: 'entregado', etiqueta: 'Entregados', cantidad: conteo['entregado'] || 0, color: 'tertiary' },
    { id: 'borrador', etiqueta: 'Borradores', cantidad: conteo['borrador'] || 0, color: 'medium' },
    { id: 'cancelado', etiqueta: 'Cancelados', cantidad: conteo['cancelado'] || 0, color: 'danger' },
  ];
});

// Contexto para estado vacío
const tipoEstadoVacio = computed(() => {
  if (offline_store.sin_conexion) return 'sin_red';
  if (terminoBusqueda.value || estadoSeleccionado.value) return 'filtros';
  return 'general';
});

const textoBotonVacio = computed(() => {
  if (offline_store.sin_conexion) return '';
  if (terminoBusqueda.value || estadoSeleccionado.value) return 'Limpiar filtros';
  return 'Crear nuevo pedido';
});

function onAccionVacio() {
  if (terminoBusqueda.value || estadoSeleccionado.value) {
    terminoBusqueda.value = '';
    estadoSeleccionado.value = null;
    cargar();
  } else {
    abrirNuevoPedido();
  }
}

function onSeleccionarEstado(id) {
  estadoSeleccionado.value = id;
  pedidos_store.filtrar_por_estado(id);
}

const TRANSICIONES_ESTADO = {
  borrador: [
    { estado: 'confirmado', texto: 'Confirmar pedido', icono: checkmarkCircleOutline },
    { estado: 'cancelado', texto: 'Cancelar pedido', icono: closeCircleOutline, role: 'destructive' },
  ],
  confirmado: [
    { estado: 'en_preparacion', texto: "Pasar a 'En preparación'", icono: timeOutline },
    { estado: 'listo', texto: "Marcar como 'Listo'", icono: checkmarkCircleOutline },
    { estado: 'cancelado', texto: 'Cancelar pedido', icono: closeCircleOutline, role: 'destructive' },
  ],
  en_preparacion: [
    { estado: 'listo', texto: "Marcar como 'Listo'", icono: checkmarkCircleOutline },
    { estado: 'cancelado', texto: 'Cancelar pedido', icono: closeCircleOutline, role: 'destructive' },
  ],
  listo: [
    { estado: 'entregado', texto: "Marcar como 'Entregado'", icono: checkmarkDoneOutline },
    { estado: 'cancelado', texto: 'Cancelar pedido', icono: closeCircleOutline, role: 'destructive' },
  ],
  entregado: [
    { estado: 'cerrado', texto: 'Cerrar pedido', icono: checkmarkDoneCircleOutline },
  ],
  cerrado: [],
  cancelado: [],
};

function obtenerTransiciones(estado) {
  const norm = normalizar_estado(estado);
  return TRANSICIONES_ESTADO[norm] || [];
}

function tieneTransiciones(estado) {
  return obtenerTransiciones(estado).length > 0;
}

async function abrirSelectorEstado(pedido) {
  await vibracion_service.vibrar_toque();
  const transiciones = obtenerTransiciones(pedido.estado);
  if (!transiciones || transiciones.length === 0) return;

  const botones = transiciones.map((t) => ({
    text: t.texto,
    icon: t.icono,
    role: t.role,
    handler: () => {
      if (t.estado === 'cancelado') {
        solicitarConfirmacionCancelacion(pedido);
      } else {
        ejecutarCambioEstado(pedido, t.estado);
      }
    },
  }));

  botones.push({
    text: 'Cancelar',
    icon: closeOutline,
    role: 'cancel',
  });

  const sheet = await actionSheetController.create({
    header: `Cambiar estado de ${formatearCodigo(pedido.id)}`,
    subHeader: `Estado actual: ${obtener_config_estado(pedido.estado).texto}`,
    buttons: botones,
  });

  await sheet.present();
}

/**
 * Confirmación explícita con alertController nombrando la entidad con botón rojo destructivo.
 */
async function solicitarConfirmacionCancelacion(pedido) {
  await vibracion_service.vibrar_toque();
  const codigo = formatearCodigo(pedido.id);
  const alerta = await alertController.create({
    header: 'Confirmar cancelación',
    subHeader: `Pedido ${codigo}`,
    message: `¿Estás seguro de que deseas cancelar el pedido ${codigo}? Esta acción no se puede revertir.`,
    buttons: [
      {
        text: 'Volver',
        role: 'cancel',
      },
      {
        text: 'Cancelar pedido',
        role: 'destructive',
        handler: () => {
          ejecutarCambioEstado(pedido, 'cancelado');
        },
      },
    ],
  });
  await alerta.present();
}

async function ejecutarCambioEstado(pedido, nuevoEstado) {
  cambiandoEstadoId.value = pedido.id;
  try {
    await pedidos_store.cambiar_estado_pedido(pedido.id, nuevoEstado);
    await vibracion_service.vibrar_toque();
    if (!offline_store.sin_conexion) {
      await notificaciones_store.cargar().catch(() => {});
    }
  } catch (error) {
    await vibracion_service.vibrar_error();
    const alerta = await alertController.create({
      header: 'Error al cambiar estado',
      message: error.mensaje || error.message || 'No se pudo actualizar el estado del pedido.',
      buttons: ['Aceptar'],
    });
    await alerta.present();
  } finally {
    cambiandoEstadoId.value = null;
  }
}

function abrirNuevoPedido() {
  vibracion_service.vibrar_toque();
  mostrarModalPedido.value = true;
}

async function onPedidoCreado(pedido) {
  await notificaciones_nativas_service.notificar_evento_local(
    'Pedido creado',
    `${pedido.codigo} - Total: $${Number(pedido.total).toLocaleString('es-AR', { minimumFractionDigits: 2 })}`
  );
  await cargar();
}

function formatearCodigo(id) {
  if (!id) return '';
  return `PED-${String(id).padStart(4, '0')}`;
}

function puedeImprimirComprobante(estado) {
  const norm = normalizar_estado(estado);
  return norm && norm !== 'borrador' && norm !== 'cancelado';
}

function resolverQrUrl(id) {
  return `${obtener_api_url()}/api/pedidos/${id}/qr`;
}

async function alternarExpansion(id) {
  pedidoExpandidoId.value = pedidoExpandidoId.value === id ? null : id;
  await vibracion_service.vibrar_toque();
}

async function cargar(event = null) {
  pedidos_store.reiniciar_paginacion();
  await pedidos_store.cargar_pedidos({
    busqueda: offline_store.sin_conexion ? '' : terminoBusqueda.value,
    estado: estadoSeleccionado.value || undefined,
  });
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
  const numero = parseInt(texto.replace(/\D/g, ''), 10);
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
        header: 'Código no reconocido',
        message: `El código escaneado ("${textoQr}") no coincide con el formato esperado de comprobante (PED-0000).`,
        buttons: ['Entendido'],
      });
      await alerta.present();
      return;
    }

    terminoBusqueda.value = `PED-${String(id).padStart(4, '0')}`;
    pedidoExpandidoId.value = id;

    await pedidos_store.cargar_pedidos({ busqueda: id.toString() });

    const encontrado = pedidos_store.pedidos.find((p) => p.id === id);
    if (encontrado) {
      pedidoExpandidoId.value = encontrado.id;
    } else {
      await pedidos_store.cargar_pedidos({ busqueda: '' });
      const localizado = pedidos_store.pedidos.find((p) => p.id === id);
      if (localizado) {
        pedidoExpandidoId.value = localizado.id;
      }
    }
  } catch (error) {
    await vibracion_service.vibrar_error();
    console.error('Error al escanear QR:', error);
    const alerta = await alertController.create({
      header: 'Error de Escaneo',
      message: error.message || 'No se pudo completar el escaneo del código.',
      buttons: ['Cerrar'],
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
      texto: `Comprobante de pedido para ${pedido.cliente?.nombre || 'Cliente'} - Veterinaria San Roque`,
    });
  } catch (error) {
    await vibracion_service.vibrar_error();
    console.error('Error al compartir comprobante:', error);
    const alerta = await alertController.create({
      header: 'No se pudo compartir',
      message: error.mensaje || error.message || 'Error al obtener el comprobante del servidor.',
      buttons: ['Aceptar'],
    });
    await alerta.present();
  } finally {
    compartiendoId.value = null;
  }
}

onMounted(() => {
  // Manejo de query param ?buscar=PED-XXXX recibido desde notificación push
  const params = new URLSearchParams(window.location.search);
  const buscarParam = params.get('buscar');
  if (buscarParam) {
    terminoBusqueda.value = buscarParam;
  }
  cargar();
});
</script>

<style scoped>
.lista-tarjetas-pedidos {
  display: flex;
  flex-direction: column;
  gap: 12px;
  margin-top: 8px;
}

.tarjeta-pedido {
  cursor: pointer;
}

.tarjeta-cabecera {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 14px 16px;
}

.tarjeta-icono-envoltorio {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 44px;
  height: 44px;
  border-radius: var(--vet-radio-chico, 10px);
  background: rgba(var(--ion-color-primary-rgb, 56, 128, 255), 0.1);
  flex-shrink: 0;
}

.tarjeta-icono-pedido {
  font-size: 24px;
  color: var(--ion-color-primary, #3880ff);
}

.tarjeta-info {
  flex: 1;
  min-width: 0;
}

.tarjeta-linea-superior {
  display: flex;
  align-items: baseline;
  gap: 6px;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.codigo-pedido {
  margin: 0;
  font-size: 0.95rem;
  font-weight: 700;
  color: var(--ion-color-step-900, #0f172a);
}

.separador-punto {
  color: var(--vet-muted, #64748b);
}

.nombre-cliente {
  font-size: 0.9rem;
  font-weight: 600;
  color: var(--ion-color-step-700, #334155);
  overflow: hidden;
  text-overflow: ellipsis;
}

.fecha-pedido {
  margin: 3px 0 0;
  font-size: 0.8rem;
  color: var(--vet-muted, #64748b);
}

.precio-pedido {
  margin: 4px 0 0;
  font-size: 1rem;
  font-weight: 700;
  color: var(--ion-color-success-shade, #1ea753);
}

.tarjeta-lateral {
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  gap: 8px;
  flex-shrink: 0;
}

.insignias-contenedor {
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  gap: 4px;
}

.badge-sin-enviar {
  font-size: 0.7rem;
  font-weight: 700;
  letter-spacing: 0.03em;
}

.icono-chevron {
  font-size: 16px;
  color: var(--ion-color-medium, #92949c);
}

/* Ficha Deslizable de Detalle */
.ficha-deslizable {
  border-top: 1px dashed var(--ion-color-step-200, #e2e8f0);
}

.ficha-cabecera-sticky {
  position: sticky;
  top: 0;
  z-index: 2;
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 12px 16px;
  border-bottom: 2px solid;
  backdrop-filter: blur(8px);
}

.ficha-cabecera-info {
  display: flex;
  flex-direction: column;
}

.ficha-subtitulo {
  font-size: 0.75rem;
  text-transform: uppercase;
  font-weight: 700;
  letter-spacing: 0.05em;
  opacity: 0.8;
}

.ficha-titulo {
  font-size: 1.1rem;
  font-weight: 800;
}

.ficha-cuerpo {
  margin-top: -12px;
  padding: 20px 16px 16px;
  background: var(--ion-card-background, #ffffff);
  border-radius: 20px 20px 0 0;
}

.seccion-items {
  margin-bottom: 12px;
}

.items-titulo {
  margin: 0 0 8px;
  font-size: 0.85rem;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.04em;
  color: var(--vet-muted, #64748b);
}

.item-fila {
  display: flex;
  justify-content: space-between;
  padding: 6px 0;
  font-size: 0.9rem;
  border-bottom: 1px solid var(--ion-color-step-100, #f1f5f9);
}

.item-desc {
  color: var(--ion-color-step-800, #1e293b);
}

.item-subtotal {
  font-weight: 700;
  color: var(--ion-color-step-900, #0f172a);
}

.seccion-notas {
  margin: 8px 0 12px;
  padding: 8px 12px;
  border-radius: 8px;
  background: var(--ion-color-step-50, #f8fafc);
  font-size: 0.85rem;
  color: var(--vet-muted, #475569);
}

.contenedor-qr {
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 14px;
  background: #ffffff;
  border-radius: var(--vet-radio, 16px);
  border: var(--vet-border, 1px solid #e2e8f0);
  margin: 12px 0;
}

.imagen-qr {
  width: 140px;
  height: 140px;
  object-fit: contain;
}

.texto-qr {
  margin-top: 8px;
  font-weight: 800;
  font-size: 1.05rem;
  letter-spacing: 0.05em;
  color: var(--ion-color-primary, #3880ff);
}

.instruccion-qr {
  font-size: 0.78rem;
  color: var(--vet-muted, #64748b);
  margin-top: 2px;
}

.ficha-acciones {
  display: flex;
  flex-direction: column;
  gap: 8px;
  margin-top: 12px;
}

.aviso-borrador {
  font-size: 0.85rem;
  color: var(--vet-muted, #64748b);
  text-align: center;
  margin: 4px 0;
  font-style: italic;
}
</style>

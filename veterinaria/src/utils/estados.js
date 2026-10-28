import {
  documentTextOutline,
  checkmarkCircleOutline,
  timeOutline,
  checkmarkDoneOutline,
  checkmarkDoneCircleOutline,
  archiveOutline,
  closeCircleOutline,
  bicycleOutline,
  navigateOutline,
  helpCircleOutline,
} from 'ionicons/icons';

export const DICCIONARIO_ESTADOS = {
  borrador: {
    clave: 'borrador',
    texto: 'Borrador',
    color: 'medium',
    icono: documentTextOutline,
    fondo_translúcido: 'rgba(146, 148, 156, 0.14)',
    color_texto: 'var(--ion-color-medium-shade, #5f5f5f)',
  },
  confirmado: {
    clave: 'confirmado',
    texto: 'Confirmado',
    color: 'primary',
    icono: checkmarkCircleOutline,
    fondo_translúcido: 'rgba(56, 128, 255, 0.14)',
    color_texto: 'var(--ion-color-primary, #3880ff)',
  },
  en_preparacion: {
    clave: 'en_preparacion',
    texto: 'En preparación',
    color: 'warning',
    icono: timeOutline,
    fondo_translúcido: 'rgba(255, 196, 9, 0.18)',
    color_texto: 'var(--ion-color-warning-shade, #b88600)',
  },
  listo: {
    clave: 'listo',
    texto: 'Listo',
    color: 'success',
    icono: checkmarkDoneOutline,
    fondo_translúcido: 'rgba(45, 211, 111, 0.16)',
    color_texto: 'var(--ion-color-success-shade, #1ea753)',
  },
  entregado: {
    clave: 'entregado',
    texto: 'Entregado',
    color: 'tertiary',
    icono: checkmarkDoneCircleOutline,
    fondo_translúcido: 'rgba(82, 96, 255, 0.16)',
    color_texto: 'var(--ion-color-tertiary, #5260ff)',
  },
  cerrado: {
    clave: 'cerrado',
    texto: 'Cerrado',
    color: 'dark',
    icono: archiveOutline,
    fondo_translúcido: 'rgba(34, 36, 40, 0.14)',
    color_texto: 'var(--ion-color-dark, #222428)',
  },
  cancelado: {
    clave: 'cancelado',
    texto: 'Cancelado',
    color: 'danger',
    icono: closeCircleOutline,
    fondo_translúcido: 'rgba(235, 68, 90, 0.14)',
    color_texto: 'var(--ion-color-danger, #eb445a)',
  },

  // Estados de Entregas
  pendiente: {
    clave: 'pendiente',
    texto: 'Pendiente',
    color: 'warning',
    icono: timeOutline,
    fondo_translúcido: 'rgba(255, 196, 9, 0.18)',
    color_texto: 'var(--ion-color-warning-shade, #b88600)',
  },
  asignada: {
    clave: 'asignada',
    texto: 'Asignada',
    color: 'primary',
    icono: bicycleOutline,
    fondo_translúcido: 'rgba(56, 128, 255, 0.14)',
    color_texto: 'var(--ion-color-primary, #3880ff)',
  },
  en_camino: {
    clave: 'en_camino',
    texto: 'En camino',
    color: 'secondary',
    icono: navigateOutline,
    fondo_translúcido: 'rgba(61, 194, 255, 0.16)',
    color_texto: 'var(--ion-color-secondary, #3dc2ff)',
  },
};

export function normalizar_estado(estado) {
  if (!estado) return '';
  return String(estado).toLowerCase().replace(/-/g, '_').trim();
}

export function obtener_config_estado(estado) {
  const norm = normalizar_estado(estado);
  return (
    DICCIONARIO_ESTADOS[norm] || {
      clave: norm,
      texto: estado ? String(estado).replace(/_/g, ' ') : 'Desconocido',
      color: 'medium',
      icono: helpCircleOutline,
      fondo_translúcido: 'rgba(146, 148, 156, 0.14)',
      color_texto: 'var(--ion-color-medium, #92949c)',
    }
  );
}

export function formatear_estado(estado) {
  return obtener_config_estado(estado).texto;
}

export function color_estado(estado) {
  return obtener_config_estado(estado).color;
}

export function icono_estado(estado) {
  return obtener_config_estado(estado).icono;
}

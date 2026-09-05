const RETARDO_SIMULADO = 600;

const CLIENTES = [
  { id: 1, nombre: 'Marta Quiroga', telefono: '266-455-1001', mascota: 'Toby (Caniche)', direccion: 'Av. Illia 340' },
  { id: 2, nombre: 'Jorge Lucero', telefono: '266-455-1002', mascota: 'Luna (Labrador)', direccion: 'Colón 545' },
  { id: 3, nombre: 'Ana Sosa', telefono: '266-455-1003', mascota: 'Mishi (Gato Siamés)', direccion: 'Junín 1120' },
  { id: 4, nombre: 'Raúl Ortega', telefono: '266-455-1004', mascota: 'Rocky (Bóxer)', direccion: null } // Caso incompleto
];

export function obtener_clientes() {
  return new Promise((resolve) => {
    setTimeout(() => {
      resolve({ clientes: CLIENTES });
    }, RETARDO_SIMULADO);
  });
}
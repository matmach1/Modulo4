// Textos de la spec para los valores que la API devuelve como nombres de enum.

const PENDIENTE = 'Pendiente de clasificación';

export const ESTADOS = {
  Abierto: 'Abierto',
  EnProgreso: 'En Progreso',
  Cerrado: 'Cerrado',
};

export const TIPOS = {
  Pendiente: PENDIENTE,
  ActoInseguro: 'Acto inseguro',
  CondicionInsegura: 'Condición insegura',
  CasiAccidente: 'Casi accidente',
  RiesgoOperativo: 'Riesgo operativo',
  Otro: 'Otro',
};

export const CATEGORIAS = {
  Pendiente: PENDIENTE,
  ProblemaInfraestructura: 'Problema de infraestructura',
  RiesgoElectrico: 'Riesgo eléctrico',
  UsoEpp: 'Uso de elementos de protección personal',
  Otro: 'Otro',
};

export const CRITICIDADES = {
  Pendiente: PENDIENTE,
  Baja: 'Baja',
  Media: 'Media',
  Alta: 'Alta',
  Critica: 'Crítica',
};

export function texto(tabla, valor) {
  return tabla[valor] ?? valor ?? '';
}

export function fechaHora(iso) {
  return iso ? new Date(iso).toLocaleString('es-AR') : '';
}

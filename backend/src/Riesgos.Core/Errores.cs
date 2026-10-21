namespace Riesgos.Core;

// Excepciones de negocio que la API traduce a códigos HTTP (contracts/api.md).

// 403: autenticado pero sin permiso sobre el recurso.
public sealed class AccesoDenegado(string mensaje = "No tenés permiso para acceder a este recurso.") : Exception(mensaje);

// 404: el recurso no existe.
public sealed class NoEncontrado(string mensaje = "El recurso no existe.") : Exception(mensaje);

// 409: conflicto de estado, duplicado o concurrencia.
public sealed class Conflicto(string mensaje) : Exception(mensaje);

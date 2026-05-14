# 🐶 DogGo - Plataforma Integral de Gestión de Paseos

> **Colas felices, dueños tranquilos: Seguridad y trazabilidad en cada paso.**

DogGo es una solución de ingeniería de software diseñada para conectar dueños de mascotas con paseadores verificados en Nuevo León. El sistema utiliza una arquitectura robusta para garantizar la seguridad, la persistencia de datos y el rendimiento bajo condiciones de carga masiva.

---

## 📊 Auditoría de Robustez y Pruebas de Estrés (Fase 5)

Como parte de la validación técnica final, el sistema fue sometido a auditorías críticas:

- **Prueba de Carga Masiva (Seeding):** Base de datos poblada con **+200 registros de paseos**, manteniendo un tiempo de respuesta profesional de **1.46s**.
- **Análisis de Latencia y Memoria:** Identificación de saturación en el hilo principal durante el renderizado masivo (detección de **202 frames saltados** vía Choreographer).
- **Optimización de Red:** Implementación de **Lazy Loading**, logrando una reducción del **40% en el tráfico de datos** y optimizando la RAM en dispositivos móviles.

---

## 🛡️ Seguridad y Blindaje Técnico

- **Seguridad Híbrida:** Migración del algoritmo de cifrado de SHA-256 a **BCrypt** en el servidor, garantizando protección total contra ataques de fuerza bruta.
- **Sanitización de Datos:** Implementación de filtros de validación mediante **RegEx** y propiedades *non-nullable* en los ViewModels para prevenir inyecciones de código y corrupción de datos.
- **Autenticación por Roles:** Gestión estricta de accesos mediante `Claims` y `Cookies Authentication` para diferenciar las funcionalidades de **Dueño** y **Paseador**.

---

## 🛠️ Stack Tecnológico

### Backend
- **C# / .NET 9**
- **ASP.NET Core MVC**
- **Entity Framework Core**
- **BCrypt.Net** (Seguridad de identidad)

### Frontend
- **Razor Views (.cshtml)** con arquitectura de modelos locales.
- **JavaScript ES6+ / Bootstrap 5**
- **Flutter (Dart)** para la terminal móvil de alta fidelidad.

### Infraestructura y Datos
- **MySQL / MariaDB** (Configurado en puerto **3307** para entorno de desarrollo).
- **Cloudflare Tunnel** para exposición segura del backend a la app móvil.
- **Ubuntu Server** para pruebas de despliegue en entornos Linux.

---

## 🏗️ Estructura del Proyecto

```
DogGo/
├── Controllers/      # Lógica de negocio y manejo de peticiones
├── Data/             # DbContext y mapeo de Entity Framework
├── Models/           # Modelos de dominio y entidades de base de datos
├── ViewModels/       # Modelos optimizados para el intercambio de datos en vistas
├── Services/         # Servicios de hashing (BCrypt) y envío de correos
├── Views/            # Vistas Razor con diseño responsivo
├── wwwroot/          # Recursos estáticos (CSS, JS, Imágenes de perros)
└── Program.cs        # Configuración de servicios y pipeline de middleware
```

---

## 🚀 Instalación y Setup

1. **Clonar repositorio:** `git clone https://github.com/GeraM0216/DogGo.git`
2. **Base de Datos:** Configurar MySQL en el puerto **3307** e importar la base de datos `doggo_db`.
3. **Dependencias:** Ejecutar `dotnet restore` para instalar paquetes de NuGet.
4. **Ejecución:** `dotnet run` para iniciar el servidor local expuesto vía Cloudflare.

---

## 🔄 Funcionamiento del Sistema (Modo Dual)

El sistema permite una transición fluida entre roles:
1. **Dueño:** Registro de mascotas  búsqueda de paseadores y visualización de evidencias de paseo.
2. **Paseador:** Gestión de disponibilidad, subida de fotos (inicio/fin) y reporte de ubicación en tiempo real.

**Estado del Repositorio:** 🟢 CERTIFICADO PARA DESPLIEGUE (FASE 5)  
**Última Actualización:** Mayo 2026

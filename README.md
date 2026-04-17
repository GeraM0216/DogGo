#  DogGo - Paseo de Perros Inteligente

> **Colas felices, dueños tranquilos**

Una aplicación web que conecta dueños de perros con paseadores verificados en Nuevo León, brindando trazabilidad digital, seguridad y tranquilidad.

---

##  Tabla de Contenidos

- [Visión General](#visión-general)
- [Características Principales](#características-principales)
- [Tecnología](#tecnología)
- [Estructura del Proyecto](#estructura-del-proyecto)
- [Instalación](#instalación)
- [Uso](#uso)
- [Equipo](#equipo)
- [Licencia](#licencia)

---

##  Visión General

DogGo es una solución innovadora para un problema real: **muchas personas en la ciudad no tienen tiempo para pasear a sus mascotas como quisieran**. 

Nuestro objetivo es:
- ✅ Conectar dueños con paseadores verificados
- ✅ Garantizar seguridad mediante trazabilidad digital
- ✅ Ofrecer protocolos automáticos de emergencia
- ✅ Crear una comunidad de amantes de perros

**Mercado Objetivo:** Nuevo León, México | **Fase Actual:** MVP

---

##  Características Principales

### Para Dueños
- Interfaz intuitiva y fácil de usar
- Búsqueda de paseadores verificados
- GPS en tiempo real durante el paseo
- Fotos y videos del paseo
- Sistema de calificaciones
- Chat en vivo con paseador
- Protocolos automáticos de emergencia

### Para Paseadores
-  Gestión flexible de horarios
-  Notificaciones en tiempo real
-  Reputación y perfil verificado

---

##  Tecnología

### Backend
- **C#**
- **ASP.NET Core MVC**
- **Entity Framework Core**
- **SignalR**

### Frontend
- **Razor Views (.cshtml)**
- **HTML**
- **CSS**
- **JavaScript**
- **Bootstrap**

### Base de datos
- **MySQL**
- **Pomelo.EntityFrameworkCore.MySql**

### Autenticación y seguridad
- **Cookie Authentication**
- confirmación de correo por código
- validaciones de formularios y acceso por rol

### Servicios auxiliares
- envío de correos electrónicos para confirmación
- **Cloudflare Tunnel** para pruebas remotas
- despliegue/pruebas en servidor Ubuntu

---

##  Estructura del Proyecto

```
DogGo/
├── Controllers/        # Controladores MVC
├── Data/               # DbContext y acceso a datos
├── Hubs/               # SignalR Hubs
├── Migrations/         # Migraciones de Entity Framework
├── Models/             # Modelos de dominio
├── Services/           # Servicios auxiliares (correo, etc.)
├── ViewModels/         # Modelos para vistas
├── Views/              # Vistas Razor
├── wwwroot/            # Archivos estáticos
├── Program.cs          # Configuración principal
├── appsettings.json    # Configuración general
└── DogGo.csproj        # Proyecto .NET
```

---

##  Instalación

### Requisitos Previos
.NET 9 SDK
MySQL 8
Visual Studio 2026 o VS Code
Git

### Local Setup

```bash
# 1. Clonar repositorio
git clone https://github.com/GeraM0216/DogGo.git
cd DogGo

# 2. Restaurar paquetes
dotnet restore

# 3. Configurar la cadena de conexión en appsettings.json

# 4. Aplicar migraciones
dotnet ef database update

# 5. Ejecutar proyecto
dotnet run
```

##  Uso

### Flujo de Dueño

1. **Crear Cuenta** → Registrar nombre, correo, teléfono y contraseña
2. **Confirmar Correo** → Ingresar el código enviado al email
3. **Registrar Perro(s)** → Nombre, raza, edad, tamaño, notas e imagen
4. **Buscar Paseador** → Revisar perfiles disponibles
5. **Ver Perfil** → Consultar experiencia, descripción y zona de servicio
6. **Solicitar Paseo** → Elegir perro, duración y tipo de paseo
7. **Dar Seguimiento** → Ver ubicación en tiempo real durante el paseo
8. **Recibir Evidencia** → Ver foto de inicio y foto final del paseo
9. **Calificar** → Dejar puntuación y comentario al finalizar

### Flujo de Paseador

1. **Crear Cuenta** → Registrar datos básicos y contraseña
2. **Confirmar Correo** → Validar cuenta con código enviado por email
3. **Completar Perfil** → Añadir foto, descripción, experiencia, tarifa y zona
4. **Revisar Paseos** → Consultar solicitudes pendientes y paseos asignados
5. **Iniciar Paseo** → Subir foto inicial y comenzar el recorrido
6. **Compartir Ubicación** → Enviar posición en tiempo real durante el paseo
7. **Finalizar Paseo** → Subir foto final y cerrar el recorrido
8. **Recibir Calificación** → Consultar reseñas dejadas por el dueño

---

##  Seguridad

- Autenticación mediante cookies en ASP.NET Core
- Confirmación de correo con código de verificación
- Autorización por roles (Dueño / Paseador)
- Validaciones de formularios en servidor
- Uso de AntiForgeryToken en formularios protegidos
- Almacenamiento seguro de contraseñas mediante hash
- Acceso a datos mediante Entity Framework Core

##  Pruebas remotas y despliegue

- Pruebas remotas mediante Cloudflare Tunnel
- Ejecución en entorno Ubuntu para validación del sistema
- Base de datos MySQL conectada al proyecto MVC

*Last Updated: Abril 2026*
*Repository Status: 🟢 ACTIVE DEVELOPMENT*

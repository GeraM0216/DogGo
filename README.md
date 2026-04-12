# 🐕 DogGo - Paseo de Perros Inteligente

> **Colas felices, dueños tranquilos**

Una plataforma móvil que conecta dueños de perros con paseadores verificados en Nuevo León, brindando trazabilidad digital, seguridad y tranquilidad.

---

## 📋 Tabla de Contenidos

- [Visión General](#visión-general)
- [Características Principales](#características-principales)
- [Tecnología](#tecnología)
- [Estructura del Proyecto](#estructura-del-proyecto)
- [Instalación](#instalación)
- [Uso](#uso)
- [Equipo](#equipo)
- [Licencia](#licencia)

---

## 🎯 Visión General

DogGo es una solución innovadora para un problema real: **muchas personas en la ciudad no tienen tiempo para pasear a sus mascotas como quisieran**. 

Nuestro objetivo es:
- ✅ Conectar dueños con paseadores verificados
- ✅ Garantizar seguridad mediante trazabilidad digital
- ✅ Ofrecer protocolos automáticos de emergencia
- ✅ Crear una comunidad de amantes de perros

**Mercado Objetivo:** Nuevo León, México | **Fase Actual:** MVP

---

## ⭐ Características Principales

### Para Dueños
- 📱 Interfaz intuitiva y fácil de usar
- 🔐 Búsqueda de paseadores verificados
- 📍 GPS en tiempo real durante el paseo
- 📸 Fotos y videos del paseo
- ⭐ Sistema de calificaciones
- 💬 Chat en vivo con paseador
- 🚨 Protocolos automáticos de emergencia

### Para Paseadores
- 📋 Gestión flexible de horarios
- 💰 Pagos automáticos (80% comisión)
- 📊 Dashboard de ganancias
- 🔔 Notificaciones en tiempo real
- 📈 Reputación y perfil verificado

### Sistema Central
- ☁️ Infraestructura en Google Cloud
- 🔐 Seguridad y encriptación de datos
- 📊 Analytics y monitoreo
- 🛡️ Seguro incluido por paseo

---

## 🛠️ Tecnología

### Frontend
- **Mobile:** React Native (iOS + Android)
- **Web:** React.js
- **UI:** Material Design + Custom Styling

### Backend
- **Runtime:** Cloud Run (Google Cloud)
- **Lenguaje:** Node.js / Python
- **API:** REST con WebSocket para GPS real-time

### Base de Datos
- **DB:** Cloud SQL (MySQL 8.0)
- **Storage:** Cloud Storage (fotos/videos)
- **Cache:** Redis (sesiones)

### Servicios Externos
- **Pagos:** Stripe / PayPal
- **Geolocalización:** Google Maps API
- **Emails:** SendGrid
- **Monitoreo:** Cloud Logging + Cloud Monitoring

### Seguridad
- SSL/TLS (HTTPS)
- JWT (autenticación)
- Encriptación de datos sensibles
- Verificación de paseadores (KYC)

---

## 📁 Estructura del Proyecto

```
DogGo/
├── Frontend/
│   ├── mobile/                 # React Native (iOS/Android)
│   ├── web/                    # React.js Web
│   └── shared/                 # Componentes compartidos
├── Backend/
│   ├── Controllers/            # Lógica de negocio
│   ├── Models/                 # Esquemas de BD
│   ├── Services/               # Servicios (GPS, Pagos, etc)
│   ├── Migrations/             # Migraciones DB
│   ├── Views/                  # Vistas de email
│   ├── Hubs/                   # WebSocket (GPS real-time)
│   ├── Properties/             # Configuración
│   └── Program.cs              # Punto de entrada
├── docs/
│   ├── diagramas/              # Diagramas Mermaid
│   ├── API.md                  # Documentación API
│   └── SETUP.md                # Guía de instalación
├── .gitignore
├── appsettings.json            # Configuración
├── DogGo.csproj                # Proyecto (.NET)
└── README.md
```

---

## 🚀 Instalación

### Requisitos Previos
- Node.js 16+
- npm / yarn
- Git
- Cuenta en Google Cloud Platform (para deployment)
- MySQL 8.0+

### Local Setup

```bash
# 1. Clonar el repositorio
git clone https://github.com/GeraM0216/DogGo.git
cd DogGo

# 2. Instalar dependencias Backend
cd Backend
npm install

# 3. Instalar dependencias Frontend (web)
cd ../Frontend/web
npm install

# 4. Instalar dependencias Mobile
cd ../mobile
npm install

# 5. Configurar variables de entorno
# Copiar .env.example a .env
# Editar con tus credenciales GCP, Stripe, etc.

# 6. Levantar servidor local
npm run dev

# 7. Levantar app web
npm start
```

---

## 💻 Uso

### Flujo de Dueño

1. **Crear Cuenta** → Email + Teléfono + Datos
2. **Registrar Perro(s)** → Raza, edad, peso, notas
3. **Buscar Paseador** → Filtrar por zona, calificación, disponibilidad
4. **Ver Perfil** → Revisar experiencia y reviews
5. **Reservar Paseo** → Seleccionar fecha/hora/duración
6. **Pagar** → Stripe / PayPal
7. **GPS Live** → Ver en tiempo real dónde está tu perro
8. **Recibir Fotos** → Paseador sube evidencia
9. **Calificar** → Dejar reseña y rating

### Flujo de Paseador

1. **Crear Cuenta** → Email + Teléfono + Documentos
2. **Verificación** → Admin revisa antecedentes
3. **Perfil** → Añadir foto, bio, horarios disponibles
4. **Recibir Solicitudes** → Notificaciones en tiempo real
5. **Aceptar/Rechazar** → Confirmación del paseo
6. **Ejecutar Paseo** → Activar GPS, tomar fotos
7. **Finalizar** → Subir evidencia, cobrar automático
8. **Recibir Reseña** → Cliente califica

---

## 👥 Equipo

**Proyecto de Ingeniería de Software - Tecmilenio**

| Rol | Nombre | Matrícula |
|-----|--------|-----------|
| 👨‍💼 Project Lead | Víctor Hugo Herrera Mata | 2763639 |
| 👨‍💻 Backend Lead | Javier Terrones Pérez | 02910455 |
| 👨‍💻 Frontend Lead | Moisés Moreno | 03057160 |
| 👨‍💻 DevOps | Marco Eugenio Zavala | 2868251 |
| 👨‍💻 QA Lead | Gerardo Molina Abarca | 3068341 |
| 👨‍💻 Developer | Miguel Ángel Zavala | 2830079 |
| 👨‍💻 Developer | Armando Hernández Ayala | 2873223 |

**Profesor:** Julio Antonio García Moreno  
**Módulo:** Proyectos de Ingeniería de Software - Módulo 1, Actividad 3

---

## 📊 Roadmap

### Fase 1 (ACTUAL - MVP)
- ✅ App móvil base (React Native)
- ✅ Autenticación y perfiles
- ✅ Búsqueda de paseadores
- ✅ Reservas y pagos básicos
- ✅ GPS en tiempo real
- ✅ Sistema de calificaciones

### Fase 2 (3 meses)
- 📅 Protocolos automáticos de emergencia
- 📅 Sistema de seguro integrado
- 📅 Dashboard de analytics
- 📅 Integración con redes sociales

### Fase 3 (6 meses)
- 🌟 Expansión a Monterrey
- 🌟 Integración IA (recomendaciones)
- 🌟 Eventos y comunidad virtual
- 🌟 Expansión estatal

---

## 🔐 Seguridad

DogGo implementa estándares de seguridad robustos:

- **Autenticación:** JWT + 2FA opcional
- **Encriptación:** AES-256 para datos sensibles
- **HTTPS:** SSL/TLS en todos los endpoints
- **Verificación:** KYC para paseadores
- **Protección:** Rate limiting, CORS, SQL injection prevention
- **Cumplimiento:** LGPD, privacidad de datos

---

## 📞 Contacto & Soporte

- **Email:** doggo@tecmilenio.mx
- **WhatsApp:** +52 1 871 113 1637
- **GitHub Issues:** [Reportar bugs](https://github.com/GeraM0216/DogGo/issues)

---

## 📄 Licencia

Este proyecto es privado y propiedad de Tecmilenio. 
**Todos los derechos reservados © 2026 DogGo Team**

---

## 🙏 Agradecimientos

- Tecmilenio University
- Google Cloud Platform
- Comunidad de desarrollo mexicana
- Todos nuestros perros beta testers 🐕

---

## 📈 Métricas de Éxito (KPIs)

| Métrica | Meta (6 meses) | Estado |
|---------|----------------|--------|
| Usuarios Registrados | 500+ | 🔄 |
| Paseadores Verificados | 50+ | 🔄 |
| Paseos Completados | 2,000+ | 🔄 |
| Satisfacción (NPS) | >70 | 🔄 |
| Retención de Usuarios | >60% | 🔄 |
| Revenue MXN | $50,000+ | 🔄 |

---

**¡Únete a la revolución del paseo de perros! 🚀🐕**

*Last Updated: Abril 2026*
*Repository Status: 🟢 ACTIVE DEVELOPMENT*

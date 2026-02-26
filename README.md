# WildHaven
WildHaven es una plataforma de gestión de reportes de vida silvestre con una API REST genérica CRUD multi-base de datos y un frontend Blazor Server. Incluye autenticación JWT, autorización por roles, notificaciones en tiempo real mediante el patrón Observer y soporte para SQL Server, PostgreSQL, MySQL y MariaDB. Program.cs:10-40 EntidadesController.cs:645-666

🏗️ Arquitectura
Backend: API web ASP.NET Core (webapicsharp) con controlador genérico EntidadesController que expone CRUD dinámico sobre cualquier tabla. EntidadesController.cs:620-641
Frontend: Aplicación Blazor Server (PresentacionWildHaven) con páginas públicas y autenticadas, incluyendo login y dashboard. Home.razor:1-35 Logeo.razor:121-176
Patrón Observer: Notificaciones a dashboard, estadísticas y usuarios cuando cambia el estado de un reporte. IObserver.cs:5-8 ActualizadorDashboard.cs:9-17 EstadisticasObserver.cs:10-35 NotificadorUsuario.cs:15-38
Base de datos: Scripts SQL para crear esquema con tablas de usuarios, roles, reportes y especies. SQLQuery1.sql:1-60
🛠️ Tecnologías
.NET 8 (ASP.NET Core, Blazor Server)
Entity Framework Core / ADO.NET según proveedor
Autenticación JWT (Bearer) Program.cs:33-65
Swagger/OpenAPI para documentación
MySQL/PostgreSQL/SQL Server/MariaDB (configurable) Program.cs:75-106
BCrypt para encriptación de campos específicos RepositorioLecturaMysqlMariaDB.cs:91-101
📦 Instalación y configuración
Clonar el repositorio.
Configurar la cadena de conexión y el proveedor de base de datos en appsettings.json (clave DatabaseProvider).
Ejecutar el script de base de datos correspondiente (ej. SQLQuery1.sql para SQL Server). SQLQuery1.sql:1-4
Restaurar paquetes NuGet y compinar la solución.
Ejecutar webapicsharp (API) y PresentacionWildHaven (frontend).
🚀 Uso
API: Explore los endpoints genéricos en /swagger o use /api/info para ayuda. EntidadesController.cs:617-641
Frontend: Inicie sesión en /login; los permisos se cargan según roles y rutas asignadas. Logeo.razor:138-166
Observers: Al cambiar un reporte, se actualizan dashboard, estadísticas y se notifica al usuario. ActualizadorDashboard.cs:9-17

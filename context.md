# Contexto del Proyecto: AntarTech Control V1 RC1

## Descripción General
AntarTech Control es una aplicación de calibración y configuración para hardware de simracing (pedaleras, botoneras, frenos de mano) orientada a la lectura nativa mediante DirectInput. El objetivo es que la aplicación no solo actúe como un visor de ejes, sino como una herramienta que permita manipular y perfeccionar la señal y grabar estos ajustes para su uso en Windows.

## Arquitectura Base
*   **Lenguaje:** C# (.NET 8.0)
*   **Framework UI:** WPF (Windows Presentation Foundation)
*   **Patrón:** MVVM (Model-View-ViewModel) con el uso de CommunityToolkit.Mvvm para comandos (RelayCommand) y bindeos.
*   **Motor de Lectura:** SharpDX.DirectInput para interceptar dispositivos HID de manera nativa.

## Estructura del Código
*   `Core/`: Modelos base genéricos (`DeviceDescriptor`).
*   `Devices/`: Implementación de scanners (actualmente `DirectInputScanner` utilizando SharpDX).
*   `Models/`: Clases de dominio como `AxisInfo` (contiene la lógica de límites, zonas muertas, inversión, suavizado y respuesta).
*   `Services/`:
    *   `MathProcessor.cs`: Realiza los cálculos matemáticos pesados en tiempo real (interpolación, filtro EMA para suavizado y aceleración/extrapolación, potencias para curvas).
    *   `ProfileManager.cs`: Serialización a JSON para cargar y guardar perfiles de configuración por GUID.
    *   `DirectInputCalibrator.cs`: Inyecta la calibración de hardware directamente en el Registro de Windows (DIOBJECTCALIBRATION) y emite notificaciones a través de `winmm.dll`.
*   `ViewModels/`: Lógica de la UI, incluyendo `MainViewModel` que actúa como orquestador general de la navegación y el refresco de dispositivos (polling loop).
*   `UI/`: Vistas modulares en XAML (`CalibrationView`, `DashboardView`, `SettingsView`).
*   `App.xaml`: Contiene todo el tema visual ("Dark Neon" fucsia y celeste), conversores (como `BoolToVis`) y diccionarios de estilos para controles (botones, sliders, toggle, scroll).

## Funcionalidades Actuales Completadas (Fase 1-3)
1.  **Detección:** Reconoce dispositivos "AntarTech" o cualquier dispositivo DirectInput y mapea todos sus ejes (X, Y, Z, Rx, Ry, Rz, Sliders).
2.  **Calibración en Interfaz:**
    *   Fijar topes mínimos, máximos y centro de forma compacta.
    *   Zonas muertas (interna y externa).
    *   Curvas de respuesta no-lineales.
    *   Suavizado EMA (-99 a +99) permitiendo filtro pasa-bajos (suave) o extrapolación (agresiva).
3.  **Aplicación Global a Windows (Opción 1 completada):** Un botón inyecta Min, Max y Center en el Registro para que todos los juegos (o joy.cpl) reconozcan la calibración lineal de inmediato sin requerir vJoy.
4.  **Gestión de Perfiles:** Autoguardado e importación/exportación manual en JSON.
5.  **UI:** Sistema de pestañas eliminado por un diseño de "filas ultra-compactas" en `CalibrationView` para facilitar la visión simultánea de múltiples ejes (pedaleras).

## Próximos Pasos (Pendientes)
*   **Fase 4 (Dispositivos no-eje):** Implementar la comunicación Serie (COM) para configurar/diagnosticar dispositivos como iFlag o Rumble motors de AntarTech.
*   **Mejoras Menores:** Integración de instalador estándar Inno Setup para Windows y distribución Open Source.

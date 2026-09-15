# Architecture - AntarTech Control V1

Este documento detalla las decisiones arquitectónicas para la V1 de AntarTech Control.

## Pipeline de Procesamiento de Ejes

La aplicación separa estrictamente la lectura de la manipulación.

`[ RAW INPUT ] -> [ CALIBRATION / PROCESSING ] -> [ PROCESSED OUTPUT ]`

1. **RAW INPUT**: El dato proveniente del dispositivo USB. Jamás es modificado directamente.
2. **PROCESSING**: Lógica matemática pura que toma el Raw Input y aplica Min/Max, Centros, Deadzones (Inner/Outer) y Curvas de respuesta.
3. **OUTPUT**: El valor final utilizado para la visualización (y eventualmente para ser inyectado/modificado a nivel de OS en la V2).

## Evaluación de APIs HID

Para la lectura de periféricos genéricos DIY (especialmente RP2040), existen 3 opciones viables en .NET 8 para Windows:

1. **Windows.Gaming.Input (RawGameController)**
   - *Ventajas*: API oficial moderna de Windows (UWP / WinRT). No requiere dependencias externas.
   - *Desventajas*: Extremadamente estricta con los Descriptores HID. Si el RP2040 no implementa un descriptor perfecto de Gamepad (Páginas de uso `0x01`, `0x05` / `0x04`), puede ignorarlo.

2. **SharpDX.DirectInput**
   - *Ventajas*: Es la API utilizada por DXTweak2 y la mayoría de simuladores clásicos. Extremadamente permisiva, lee cualquier cosa que Windows catalogue como Joystick/Gamepad.
   - *Desventajas*: El paquete `SharpDX` está obsoleto (deprecated) y no recibe actualizaciones, aunque funciona perfectamente en Windows 10/11.

3. **HidLibrary / P/Invoke a hid.dll**
   - *Ventajas*: Acceso puro a nivel bajo de bytes al dispositivo HID.
   - *Desventajas*: Requiere parsear el Descriptor HID a mano, lo cual es complejo y propenso a errores al leer ejes que varían de 8 a 16 bits según el firmware del dispositivo.

**Decisión**: La implementación realizará una prueba inicial priorizando **DirectInput**, ya que es la única que garantiza leer correctamente el rango RAW crudo de ejes con mapeos personalizados en dispositivos DIY (el objetivo principal del proyecto), y es 100% compatible con los resultados que daría el clásico DXTweak. Si se requiere HID crudo (por ejemplo, Feature Reports para configuración en V2), se utilizará `HidLibrary` en paralelo solo para enviar/recibir configuración, y `DirectInput` para leer los ejes a 120Hz.

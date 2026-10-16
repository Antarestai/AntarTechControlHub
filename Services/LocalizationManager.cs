using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AntarTechControl.Services
{
    public class LocalizationManager : INotifyPropertyChanged
    {
        public static LocalizationManager Instance { get; } = new LocalizationManager();

        private string _currentLanguage = "Español";
        public string CurrentLanguage
        {
            get => _currentLanguage;
            set
            {
                if (_currentLanguage != value)
                {
                    _currentLanguage = value;
                    OnPropertyChanged(string.Empty); // Notifies all localized properties at once
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool IsEn => CurrentLanguage == "English";
        private bool IsPt => CurrentLanguage == "Português";

        // Navigation
        public string NavDevices => IsEn ? "Devices" : IsPt ? "Dispositivos" : "Dispositivos";
        public string NavTutorial => IsEn ? "Tutorial" : IsPt ? "Tutorial" : "Tutorial";
        public string NavSettings => IsEn ? "Settings" : IsPt ? "Configurações" : "Configuración";

        // Dashboard
        public string DashTitle => IsEn ? "My Devices" : IsPt ? "Meus Dispositivos" : "Mis Dispositivos";
        public string DashSubtitle => "";
        public string DashScan => IsEn ? "Scan Devices" : IsPt ? "Buscar Dispositivos" : "Buscar Dispositivos";
        public string DashEmptyTitle => IsEn ? "No devices detected" : IsPt ? "Nenhum periférico detectado" : "No se detectan periféricos";
        public string DashEmptyDesc => IsEn ? "Connect your pedals, handbrake or USB adapter and click 'Scan Devices'." 
                                           : IsPt ? "Conecte seus pedais, freio de mão ou adaptador USB e clique em 'Buscar Dispositivos'." 
                                           : "Conecta tus pedales, freno de mano o adaptador USB a tu PC y haz clic en 'Buscar Dispositivos'.";
        public string DashScanNow => IsEn ? "Scan Now" : IsPt ? "Buscar Agora" : "Buscar Ahora";
        public string ConnectedDirectInput => IsEn ? "Connected via DirectInput" : IsPt ? "Conectado via DirectInput" : "Conectado vía DirectInput";
        public string CalibrateBtn => IsEn ? "CALIBRATE" : IsPt ? "CALIBRAR" : "CALIBRAR";

        // Calibration View
        public string CalibBack => IsEn ? "Devices" : IsPt ? "Dispositivos" : "Dispositivos";
        public string CalibGuide => IsEn ? "📖 Guide" : IsPt ? "📖 Guia" : "📖 Guía";
        public string CalibSaveWindows => IsEn ? "SAVE TO WINDOWS" : IsPt ? "SALVAR NO WINDOWS" : "GUARDAR EN WINDOWS";
        public string CalibReload => IsEn ? "Reload" : IsPt ? "Recarregar" : "Recargar";
        public string CalibReset => IsEn ? "Reset Defaults" : IsPt ? "Redefinir" : "Restablecer";
        public string CalibAxesTitle => IsEn ? "Hardware Axes Detected" : IsPt ? "Eixos Detectados no Hardware" : "Ejes Detectados en Hardware";
        public string CalibAxesDesc => IsEn ? "Calibrate any axis directly or select one to fine-tune" 
                                            : IsPt ? "Calibre qualquer eixo diretamente ou selecione para ajustar" 
                                            : "Calibra cualquier eje directamente o haz clic en uno para afinarlo";
        public string LegendRaw => IsEn ? "Raw Sensor" : IsPt ? "Sensor Bruto (Raw)" : "Sensor Crudo (Raw)";
        public string LegendDirectInput => IsEn ? "DirectInput (Windows)" : IsPt ? "DirectInput (Windows)" : "DirectInput (Windows)";
        public string StatusInWindows => IsEn ? "✔ In Windows" : IsPt ? "✔ No Windows" : "✔ En Windows";
        public string StatusFactory => IsEn ? "Factory" : IsPt ? "Fábrica" : "Fábrica";
        public string MinLabel => IsEn ? "MIN" : IsPt ? "MÍN" : "MÍN";
        public string CtrLabel => IsEn ? "CTR" : IsPt ? "CTR" : "CTR";
        public string MaxLabel => IsEn ? "MAX" : IsPt ? "MÁX" : "MÁX";
        public string DeadzoneLabel => IsEn ? "DEADZONE" : IsPt ? "ZONA MORTA" : "ZONA MUERTA";
        public string SetMinBtn => IsEn ? "Set Min" : IsPt ? "Fixar Mín" : "Fijar Mín";
        public string SetCenterBtn => IsEn ? "Set Cen" : IsPt ? "Fixar Cen" : "Fijar Cen";
        public string SetMaxBtn => IsEn ? "Set Max" : IsPt ? "Fixar Máx" : "Fijar Máx";
        public string ResetBtn => IsEn ? "↺ Reset" : IsPt ? "↺ Reset" : "↺ Reset";
        public string PrecisionTuning => IsEn ? "PRECISION TUNING" : IsPt ? "AJUSTE FINO" : "AFINACIÓN PRECISA";
        public string NumericalCalib => IsEn ? "Numerical Calibration" : IsPt ? "Calibração Numérica" : "Calibración Numérica";
        public string SetMinFull => IsEn ? "Set Minimum (0%)" : IsPt ? "Fixar Mínimo (0%)" : "Fijar Mínimo (0%)";
        public string SetCenterFull => IsEn ? "Set Center (50%)" : IsPt ? "Fixar Centro (50%)" : "Fijar Centro (50%)";
        public string SetMaxFull => IsEn ? "Set Maximum (100%)" : IsPt ? "Fixar Máximo (100%)" : "Fijar Máximo (100%)";
        public string ResetAxisFull => IsEn ? "Reset Axis" : IsPt ? "Redefinir Eixo" : "Restablecer Eje";
        public string AxisRoleTitle => IsEn ? "Axis Function" : IsPt ? "Função do Eixo" : "Función del Eje";
        public string AxisRoleSub => IsEn ? "Assign pedal or control" : IsPt ? "Atribuir pedal ou controle" : "Asignar pedal o control";
        public string InvertBtn => IsEn ? "⇄ Invert" : IsPt ? "⇄ Inverter" : "⇄ Invertir";
        public string InvertTooltip => IsEn ? "Invert pedal direction (swaps Min and Max)" : IsPt ? "Inverter direção do pedal (troca Mín e Máx)" : "Invierte la dirección del pedal (intercambia Mínimo y Máximo)";

        // Settings View (Logitech G HUB Minimalist & Compact)
        public string SettingsTitle => IsEn ? "General Settings" : IsPt ? "Configuração geral" : "Configuración general";
        public string StartWithWindows => IsEn ? "Launch application at system startup" : IsPt ? "Iniciar o aplicativo na inicialização" : "Iniciar la aplicación al iniciar sesión";
        public string StartWithWindowsSub => IsEn ? "Always start after logging in" : IsPt ? "Iniciar sempre após o login" : "Empezar siempre tras iniciar sesión";
        public string MinimizeTray => IsEn ? "Minimize to system tray" : IsPt ? "Minimizar para a bandeja do sistema" : "Minimizar a la bandeja del sistema";
        public string MinimizeTraySub => IsEn ? "Keep running in background when closed" : IsPt ? "Manter em segundo plano ao fechar a janela" : "Mantener en segundo plano al cerrar la ventana";
        public string PollingRateTitle => IsEn ? "Sensor Polling Rate" : IsPt ? "Taxa de Atualização dos Sensores" : "Frecuencia de lectura";
        public string PollingRateSub => IsEn ? "High-speed sensor refresh rate" : IsPt ? "Taxa de atualização para sensores" : "Tasa de refresco en la lectura de sensores de alta velocidad";
        public string LangSection => IsEn ? "Language" : IsPt ? "Idioma" : "Idioma";
        public string SupportSection => IsEn ? "Support & Feedback" : IsPt ? "Suporte e Comentários" : "Soporte técnico";
        public string SupportSub => IsEn ? "Need assistance or found a bug?" : IsPt ? "Precisa de ajuda ou encontrou um erro?" : "¿Tienes algún problema con la calibración o encontraste un fallo?";
        public string ReportErrorBtn => IsEn ? "Report Bug" : IsPt ? "Reportar Erro" : "Reportar error";
        public string CopyDiagBtn => IsEn ? "Copy Diagnostics" : IsPt ? "Copiar Diagnóstico" : "Copiar diagnóstico";
        public string AboutSection => IsEn ? "About" : IsPt ? "Sobre" : "Acerca de";
        public string AppVersionInfo => IsEn ? "AntarTech Control Suite v1.0.0 (x64) • Official Release" : IsPt ? "AntarTech Control Suite v1.0.0 (x64) • Oficial" : "AntarTech Control Suite v1.0.0 (x64) • Oficial";
        public string DevelopedBy => IsEn ? "Developed by Antares" : IsPt ? "Desenvolvido por Antares" : "Desarrollado por Antares";
        public string CheckUpdatesBtn => IsEn ? "Check for updates" : IsPt ? "Verificar atualizações" : "Buscar actualizaciones";
        public string CheckingUpdates => IsEn ? "Checking for updates..." : IsPt ? "Verificando atualizações..." : "Comprobando actualizaciones...";
        public string AppUpToDate => IsEn ? "✔ AntarTech Control is up to date (v1.0.0)" : IsPt ? "✔ AntarTech Control está atualizado (v1.0.0)" : "✔ AntarTech Control está en su versión más reciente (v1.0.0)";
        public string UpdateFoundTitle => IsEn ? "Update Available" : IsPt ? "Atualização Disponível" : "Actualización Disponible";
        public string UpdateFoundMsg => IsEn ? "A new version ({0}) is available!\n\nDo you want to visit the AntarTech website to download it?" : IsPt ? "Uma nova versão ({0}) está disponível!\n\nDeseja acessar o site da AntarTech para baixá-la?" : "¡Una nueva versión ({0}) está disponible!\n\n¿Deseas visitar el sitio web de AntarTech para descargarla?";

        // Tutorial View
        public string TutHeader => IsEn ? "OFFICIAL QUICK GUIDE" : IsPt ? "GUIA RÁPIDO OFICIAL" : "GUÍA OFICIAL RÁPIDA";
        public string TutTitle => IsEn ? "Calibration in 3 Simple Steps" : IsPt ? "Calibração em 3 Passos Simples" : "Calibración en 3 Simples Pasos";
        public string TutSubtitle => IsEn ? "Learn how to calibrate your pedals, load cells and handbrake permanently for Windows and all sims." 
                                          : IsPt ? "Aprenda a calibrar seus pedais, células de carga e freio de mão definitivamente para o Windows e todos os simuladores." 
                                          : "Aprende a calibrar tus pedales, celdas de carga y freno de mano de forma definitiva para Windows y tus simuladores.";
        public string TutStep1Title => IsEn ? "1. Connect Device via USB" : IsPt ? "1. Conectar Dispositivo via USB" : "1. Conectar Periférico por USB";
        public string TutStep1Desc => IsEn ? "Connect your pedals, handbrake or adapter via USB. AntarTech detects the hardware directly at native DirectInput level without extra drivers." 
                                           : IsPt ? "Conecte seus pedais, freio de mão ou adaptador via USB. O AntarTech detecta o hardware nativamente em DirectInput sem drivers intermediários." 
                                           : "Conecta tu pedalera, freno de mano o adaptador Logitech por USB a la PC. AntarTech detecta el hardware de forma directa a nivel DirectInput nativo sin drivers intermedios.";
        public string TutStep1Tip => IsEn ? "💡 If you connected the device with the app open, click 'Scan Devices'." 
                                          : IsPt ? "💡 Se você conectou o dispositivo com o app aberto, clique em 'Buscar Dispositivos'." 
                                          : "💡 Si conectaste el dispositivo con el programa ya abierto, pulsa 'Buscar Dispositivos'.";
        public string TutStep2Title => IsEn ? "2. Calibrate Physical Range (Min, Center, Max)" 
                                            : IsPt ? "2. Calibrar Faixa Física (Mínimo, Centro, Máximo)" 
                                            : "2. Calibrar Rango Físico (Mínimo, Centro, Máximo)";
        public string TutStep2Min => IsEn ? "• Released pedal at 0%: Click [Set Min]" : IsPt ? "• Pedal solto em 0%: Clique em [Fixar Mín]" : "• Pedal suelto al 0%: Haz clic en [Fijar Mín]";
        public string TutStep2Max => IsEn ? "• Fully pressed pedal at 100%: Click [Set Max]" : IsPt ? "• Pedal pressionado a 100%: Clique em [Fixar Máx]" : "• Pedal presionado a fondo al 100%: Haz clic en [Fijar Máx]";
        public string TutStep2Ctr => IsEn ? "• Wheels / Neutral center: Center axis and click [Set Cen]" : IsPt ? "• Volantes / Centro neutro: Alinhe o eixo e clique em [Fixar Cen]" : "• Volantes / Centro neutro: Centra el eje y pulsa [Fijar Cen]";
        public string TutStep2Tip => IsEn ? "💡 For Load Cell brakes, calibrate with the maximum race pressure you can comfortably repeat." 
                                          : IsPt ? "💡 Em freios com Célula de Carga, calibre com a força máxima de corrida que possa repetir confortavelmente." 
                                          : "💡 En frenos con Celda de Carga, calibra con la fuerza máxima de carrera que puedas repetir vuelta tras vuelta sin fatigarte.";
        public string TutStep3Title => IsEn ? "3. Save Permanently to Windows" : IsPt ? "3. Salvar no Windows Definitivamente" : "3. Guardar en Windows de Forma Definitiva";
        public string TutStep3Desc => IsEn ? "Click [SAVE TO WINDOWS]. Values are saved directly to Windows Registry and notify all games. Simulators like iRacing, Assetto Corsa, ACC, F1 and joy.cpl will read your calibration automatically." 
                                           : IsPt ? "Clique em [SALVAR NO WINDOWS]. Os valores são gravados diretamente no Registro do Windows e notificam todos os jogos. Simuladores como iRacing, Assetto Corsa, ACC, F1 e joy.cpl lerão sua calibração automaticamente." 
                                           : "Haz clic en [GUARDAR EN WINDOWS]. Los valores se graban directamente en el Registro de Windows vía DirectInput y notifican a todos los juegos. Simuladores como iRacing, Assetto Corsa Competizione, F1, Automobilista 2 y joy.cpl leerán tu calibración automáticamente.";
        public string TutStartNow => IsEn ? "Start Calibration Now →" : IsPt ? "Começar a Calibrar Agora →" : "Comenzar a Calibrar Ahora →";
    }
}

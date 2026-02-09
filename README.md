# Scripting 2026

## Español

### Instrucciones de Instalación y Configuración

Este proyecto está desarrollado con Unity y utiliza el Corgi Engine. Sigue estos pasos para configurar el proyecto en tu computadora:

#### Prerrequisitos
- **Unity Hub**: Descarga desde [unity.com](https://unity.com/download)
- **Unity 2022.3 LTS** (o la versión específica del proyecto)
- **Git**: Descarga desde [git-scm.com](https://git-scm.com/downloads)
- **Visual Studio Community** o **Visual Studio Code** para editar scripts

#### Pasos de Instalación

1. **Crear tu repositorio**
   ```bash
   # Navega a tu carpeta de proyectos
   cd tu-carpeta-de-proyectos
   
   # Clona este repositorio
   git clone https://github.com/xaca/scripting2026.git
   
   # Cambia el nombre de la carpeta si lo deseas
   mv scripting2026 tu-nuevo-nombre-proyecto
   cd tu-nuevo-nombre-proyecto
   
   # Opcional: Crea tu propio repositorio en GitHub
   # Elimina la conexión al repositorio original
   rm -rf .git
   
   # Inicializa tu nuevo repositorio
   git init
   git add .
   git commit -m "Primer commit: proyecto base"
   
   # Conecta con tu repositorio de GitHub (reemplaza con tu URL)
   git remote add origin https://github.com/tu-usuario/tu-repositorio.git
   git push -u origin main
   ```

2. **Abrir el proyecto en Unity**
   - Abre Unity Hub
   - Haz clic en "Open" o "Abrir"
   - Navega hasta la carpeta del proyecto clonado
   - Selecciona la carpeta raíz del proyecto
   - Unity detectará automáticamente el proyecto

3. **Verificar la instalación**
   - Espera a que Unity importe todos los assets
   - Abre la escena principal en `Assets/Juego/` o `Assets/CorgiEngine/`
   - Presiona el botón Play para verificar que todo funciona

#### Estructura del Proyecto
- `Assets/CorgiEngine/`: Archivos del motor Corgi Engine
- `Assets/Juego/`: Tus scripts y assets personalizados
- `Assets/MMData/`: Datos y configuraciones
- `ProjectSettings/`: Configuración del proyecto Unity

#### Solución de Problemas Comunes
- **Error de versión de Unity**: Asegúrate de usar Unity 2022.3 LTS
- **Assets faltantes**: Verifica que todos los archivos se descargaron correctamente
- **Errores de compilación**: Espera a que Unity termine de importar todos los packages

---

## English

### Installation and Setup Instructions

This project is developed with Unity and uses the Corgi Engine. Follow these steps to set up the project on your computer:

#### Prerequisites
- **Unity Hub**: Download from [unity.com](https://unity.com/download)
- **Unity 2022.3 LTS** (or the specific version required by the project)
- **Git**: Download from [git-scm.com](https://git-scm.com/downloads)
- **Visual Studio Community** or **Visual Studio Code** for script editing

#### Installation Steps

1. **Create your repository**
   ```bash
   # Navigate to your projects folder
   cd your-projects-folder
   
   # Clone this repository
   git clone https://github.com/xaca/scripting2026.git
   
   # Rename the folder if desired
   mv scripting2026 your-new-project-name
   cd your-new-project-name
   
   # Optional: Create your own repository on GitHub
   # Remove connection to original repository
   rm -rf .git
   
   # Initialize your new repository
   git init
   git add .
   git commit -m "First commit: base project"
   
   # Connect to your GitHub repository (replace with your URL)
   git remote add origin https://github.com/your-username/your-repository.git
   git push -u origin main
   ```

2. **Open the project in Unity**
   - Open Unity Hub
   - Click "Open"
   - Navigate to the cloned project folder
   - Select the project root folder
   - Unity will automatically detect the project

3. **Verify installation**
   - Wait for Unity to import all assets
   - Open the main scene in `Assets/Juego/` or `Assets/CorgiEngine/`
   - Press the Play button to verify everything works

#### Project Structure
- `Assets/CorgiEngine/`: Corgi Engine files
- `Assets/Juego/`: Your custom scripts and assets
- `Assets/MMData/`: Data and configurations
- `ProjectSettings/`: Unity project settings

#### Common Troubleshooting
- **Unity version error**: Make sure you're using Unity 2022.3 LTS
- **Missing assets**: Verify all files downloaded correctly
- **Compilation errors**: Wait for Unity to finish importing all packages

---

## Recursos Adicionales / Additional Resources

- [Unity Documentation](https://docs.unity3d.com/)
- [Corgi Engine Documentation](https://corgi-engine-docs.moremountains.com/)
- [Git Tutorial](https://git-scm.com/docs/gittutorial)

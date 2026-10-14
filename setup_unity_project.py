#!/usr/bin/env python3
"""
Unity项目设置脚本
自动创建Unity项目结构和基本配置
"""

import os
import sys
import shutil
from pathlib import Path

# 项目配置
PROJECT_NAME = "TimeOfFragments"
ASSETS_DIR = "Assets"
SCENES_DIR = os.path.join(ASSETS_DIR, "Scenes")
SCRIPTS_DIR = os.path.join(ASSETS_DIR, "Scripts")
PROJECT_SETTINGS_DIR = "ProjectSettings"

def create_directory_structure():
    """创建Unity项目目录结构"""
    print("创建目录结构...")

    directories = [
        PROJECT_SETTINGS_DIR,
        ASSETS_DIR,
        SCENES_DIR,
        SCRIPTS_DIR,
        os.path.join(SCRIPTS_DIR, "Dialogue"),
        os.path.join(SCRIPTS_DIR, "Insomnia"),
        os.path.join(SCRIPTS_DIR, "TimeLine"),
        os.path.join(SCRIPTS_DIR, "Ending"),
        os.path.join(SCRIPTS_DIR, "Audio"),
        os.path.join(SCRIPTS_DIR, "RogueLike"),
        os.path.join(SCRIPTS_DIR, "Test"),
        os.path.join(SCRIPTS_DIR, "Data"),
    ]

    for directory in directories:
        Path(directory).mkdir(parents=True, exist_ok=True)
        print(f"  创建目录: {directory}")

    print("✓ 目录结构创建完成\n")

def create_unity_project_file():
    """创建Unity项目文件"""
    print("创建Unity项目文件...")

    # 创建ProjectSettings目录
    os.makedirs(PROJECT_SETTINGS_DIR, exist_ok=True)

    # 创建project.asset文件
    project_content = """%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!86 &86
BuildSettings:
  m_ObjectHideFlags: 0
  serializedVersion: 3
  m_Name:
  scene: {00000000,00000000,00000000}
  version: 2
--- !u!114 &114
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {00000000,00000000,00000000}
  m_PrefabInstance: {00000000,00000000,00000000}
  m_PrefabAsset: {00000000,00000000,00000000}
  m_GameObject: {00000000,00000000,00000000}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {05000000,00000000,00000000}
  m_Name:
  m_EditorClassIdentifier:
  productName: TimeOfFragments
  productVersion: 1.0.0
  defaultScreenWidth: 1920
  defaultScreenHeight: 1080
  defaultScreenWidthMode: 0
  defaultScreenHeightMode: 0
  defaultIsNativeResolution: 1
  runInBackground: 1
  captureSingleScreen: 0
  muteOtherAudioSources: 0
  Prepare IOS For Recording: 0
  iPhone RunInBackground: 1
  Screen Orientation: 1
  Default Is Native Resolution: 1
  Target Pixel Ratio: 1
  Hardware Accelerated Rendering: 1
  Auto Graphics API: 1
  Target Frame Rate: -1
  Api Compatibility Level: 3
  Force Single Instance: 0
  Use Script Call Optimization: 1
  Use Marker Local Jobs: 1
  Use D3D11 Threading Model: 1
  Use Multithreaded Job System: 1
  Enable OpenGL Profiler: 0
  Use 32 Bit Viewport: 1
  Cloud Enabled: 0
  Color Space: 0
  Physics Enabled: 1
  Baked Lightmap Support: 1
  Responsive Cursor: 0
  AllowHDRDisplay: 0
  Use HDR Display: 0
  WebGL Memory Size: 256
  Android Target Screen Size: 0
  Android Min Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 0
  Android Target Screen Size: 场景/MainScene
"""

    project_file = os.path.join(PROJECT_SETTINGS_DIR, "project.asset")
    with open(project_file, 'w', encoding='utf-8') as f:
        f.write(project_content)

    print(f"✓ 创建项目文件: {project_file}\n")

def create_package_manifest():
    """创建Package Manifest"""
    print("创建Package Manifest...")

    manifest_content = """{
  "name": "com.timeoffragments.game",
  "version": "1.0.0",
  "displayName": "Time of Fragments",
  "description": "A visual novel game with time travel mechanics",
  "dependencies": {
  }
}
"""

    manifest_file = os.path.join(ASSETS_DIR, "Packages", "manifest.json")
    os.makedirs(os.path.dirname(manifest_file), exist_ok=True)
    with open(manifest_file, 'w', encoding='utf-8') as f:
        f.write(manifest_content)

    print(f"✓ 创建Package Manifest: {manifest_file}\n")

def create_gitignore():
    """创建.gitignore文件"""
    print("创建.gitignore文件...")

    gitignore_content = """# Unity
[Oo]bj/
[Bb]uild/
[Bb]uilds/
[Ll]ogs/
[Ll]ibrary/
[Tt]emp/
[Oo]ps/
[Uu]ser/
[Rr]epositories/
[Rr]esources/
[Aa]ssets/[Aa]ssets.*
[Dd]ata/
[Tt]emp/
[Ww]orkspace/
.vs/
*.csproj
*.sln
*.unity
*.prefab
*.meta
*.dll
*.exe
*.pdb
*.user
*.userprefs
*.pidb
*.booproj
*.svd
*.pdbgz
*.meta
*.suo
*.cache
*.ilk
*.log
*.mxp
*.plg
*.svd
*.pdb
*.pdbg
*.user
*.userosscache
*.sln.docstates
.vscode/
.idea/
*.swp
*.swo
*~
.DS_Store
"""

    gitignore_file = ".gitignore"
    with open(gitignore_file, 'w', encoding='utf-8') as f:
        f.write(gitignore_content)

    print(f"✓ 创建.gitignore文件: {gitignore_file}\n")

def create_main_scene():
    """创建主场景文件"""
    print("创建主场景文件...")

    # 场景文件内容（简化版）
    scene_content = """%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!29 &1
OcclusionCullingSettings:
  m_ObjectHideFlags: 0
  serializedVersion: 2
  m_OcclusionBakeSettings:
    smallestOccluder: 5
    smallestHole: 0.25
    backfaceThreshold: 100
  m_SceneGUID: 00000000000000000000000000000000
  m_OcclusionCullingData: {00000000,00000000,00000000}
--- !u!104 &2
RenderSettings:
  m_ObjectHideFlags: 0
  serializedVersion: 2
  m_Fog: 0
  m_FogColor: {r: 0.5, g: 0.5, b: 0.5, a: 1}
  m_FogMode: 3
  m_FogDensity: 0.01
  m_LinearFogStart: 0
  m_LinearFogEnd: 300
  m_AmbientSkyColor: {r: 0.2, g: 0.2, b: 0.2, a: 1}
  m_AmbientEquatorColor: {r: 0.2, g: 0.2, b: 0.2, a: 1}
  m_AmbientGroundColor: {r: 0.2, g: 0.2, b: 0.2, a: 1}
  m_AmbientIntensity: 1
  m_AmbientMode: 3
  m_SubtractiveShadowColor: {r: 0.4, g: 0.4, b: 0.4, a: 1}
  m_SkyboxMaterial: {00000000,00000000,00000000}
  m_HaloStrength: 0.5
  m_FlareStrength: 1
  m_FlareFadeSpeed: 3
  m_HaloTexture: {00000000,00000000,00000000}
  m_SpotCookie: {00000000,00000000,00000000}
  m_DefaultReflectionMode: 0
  m_DefaultReflectionResolution: 128
  m_ReflectionBounces: 1
  m_ReflectionIntensity: 1
  m_CustomReflection: {00000000,00000000,00000000}
  m_Sun: {00000000,00000000,00000000}
  m_IndirectSpecularColor: {r: 0, g: 0, b: 0, a: 1}
--- !u!157 &3
LightmapSettings:
  m_ObjectHideFlags: 0
  serializedVersion: 12
  m_GIWorkflowMode: 1
  m_GISettings:
    serializedVersion: 2
    m_BounceScale: 1
    m_IndirectOutputScale: 1
    m_AlbedoBoost: 1
    m_EnvironmentLightingMode: 0
    m_EnableBakedLightmaps: 1
    m_EnableRealtimeLightmaps: 0
  m_LightmapEditorSettings:
    serializedVersion: 12
    m_Resolution: 2
    m_BakeResolution: 40
    m_AtlasSize: 1024
    m_AO: 0
    m_AOMaxDistance: 1
    m_CompAOExponent: 1
    m_CompAOExponentDirect: 0
    m_ExtractAmbientOcclusion: 0
    m_Padding: 2
    m_LightmapParameters: {00000000,00000000,00000000}
    m_LightmapsBakeMode: 1
    m_TextureCompression: 1
    m_FinalGather: 0
    m_FinalGatherRayCount: 256
    m_ReflectionCompression: 2
    m_MixedBakeMode: 2
    m_BakeBackend: 0
    m_PVRSampling: 1
    m_PVRDirectSampleCount: 32
    m_PVRSampleCount: 512
    m_PVRBounces: 2
    m_PVREnvironmentSampleCount: 256
    m_PVREnvironmentReferencePointCount: 2048
    m_PVRFilteringMode: 1
    m_PVRDenoiserTypeDirect: 1
    m_PVRDenoiserTypeIndirect: 1
    m_PVRDenoiserTypeAO: 1
    m_PVRFilterTypeDirect: 0
    m_PVRFilterTypeIndirect: 0
    m_PVRFilterTypeAO: 0
    m_PVREnvironmentMIS: 1
    m_PVRCulling: 1
    m_PVRFilteringGaussRadiusDirect: 1
    m_PVRFilteringGaussRadiusIndirect: 5
    m_PVRFilteringGaussRadiusAO: 2
    m_PVRFilteringAtrousPositionSigmaDirect: 0.5
    m_PVRFilteringAtrousPositionSigmaIndirect: 2
    m_PVRFilteringAtrousPositionSigmaAO: 1
    m_ExportTrainingData: 0
  m_LightingDataAsset: {00000000,00000000,00000000}
  m_LightingSettings: {00000000,00000000,00000000}
--- !u!196 &4
NavMeshSettings:
  serializedVersion: 2
  m_ObjectHideFlags: 0
  m_BuildSettings:
    serializedVersion: 2
    agentTypeID: 0
    agentRadius: 0.5
    agentHeight: 2
    agentSlope: 45
    agentClimb: 0.4
    ledgeDropHeight: 0
    maxJumpAcrossDistance: 0
    minRegionArea: 2
    manualCellSize: 0
    cellSize: 0.16666667
    manualTileSize: 0
    tileSize: 256
    accuratePlacement: 0
    maxNavMeshAreaSize: 0
  m_NavMeshData: {00000000,00000000,00000000}
--- !u!1 &100000000
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {00000000,00000000,00000000}
  m_PrefabInstance: {00000000,00000000,00000000}
  m_PrefabAsset: {00000000,00000000,00000000}
  serializedVersion: 6
  m_Component:
  - component: {00000000,00000000,00000000}
  - component: {00000000,00000000,00000000}
  m_Layer: 0
  m_Name: MainCamera
  m_TagString: MainCamera
  m_Icon: {00000000,00000000,00000000}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &400000000
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {00000000,00000000,00000000}
  m_PrefabInstance: {00000000,00000000,00000000}
  m_PrefabAsset: {00000000,00000000,00000000}
  m_GameObject: {00000000,00000000,00000000}
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 1, z: -10}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_Children: []
  m_Father: {00000000,00000000,00000000}
  m_RootOrder: 0
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
--- !u!20 &200000000
Camera:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {00000000,00000000,00000000}
  m_PrefabInstance: {00000000,00000000,00000000}
  m_PrefabAsset: {00000000,00000000,00000000}
  m_GameObject: {00000000,00000000,00000000}
  m_Enabled: 1
  serializedVersion: 2
  m_ClearFlags: 1
  m_BackGroundColor: {r: 0.19215687, g: 0.3019608, b: 0.4745098, a: 0}
  m_projectionMatrixMode: 1
  m_GateFitMode: 2
  m_FOVAxisMode: 0
  m_SensorSize: {x: 36, y: 24}
  m_LensShift: {x: 0, y: 0}
  m_FocalLength: 50
  m_NormalizedViewPortRect:
    serializedVersion: 2
    x: 0
    y: 0
    width: 1
    height: 1
  near clip plane: 0.3
  far clip plane: 1000
  field of view: 60
  orthographic: 0
  orthographic size: 5
  m_Depth: -1
  m_CullingMask:
    serializedVersion: 2
    m_Bits: 4294967295
  m_RenderingPath: -1
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 0
  m_TargetTexture: {00000000,00000000,00000000}
  m_TargetDisplay: 开发中 - 请等待Unity项目创建完成
"""

    scene_file = os.path.join(SCENES_DIR, "MainScene.unity")
    os.makedirs(SCENES_DIR, exist_ok=True)
    with open(scene_file, 'w', encoding='utf-8') as f:
        f.write(scene_content)

    print(f"✓ 创建场景文件: {scene_file}\n")

def main():
    """主函数"""
    print("=" * 60)
    print("Unity项目设置脚本")
    print("项目名称: TimeOfFragments")
    print("=" * 60)
    print()

    try:
        create_directory_structure()
        create_unity_project_file()
        create_package_manifest()
        create_gitignore()
        create_main_scene()

        print("=" * 60)
        print("✓ Unity项目设置完成!")
        print("=" * 60)
        print()
        print("项目结构已创建:")
        print(f"  - {PROJECT_SETTINGS_DIR}/")
        print(f"  - {ASSETS_DIR}/")
        print(f"  - {SCENES_DIR}/")
        print(f"  - {SCRIPTS_DIR}/")
        print()
        print("下一步:")
        print("  1. 打开Unity Hub")
        print("  2. 创建新项目，选择'3D'模板")
        print("  3. 将Assets/Scripts/中的所有C#脚本复制到Unity项目的Assets/Scripts/目录")
        print("  4. 在Unity中创建一个新场景")
        print("  5. 将TestDemoScene.cs添加到场景中")
        print("  6. 运行项目")
        print()

    except Exception as e:
        print(f"错误: {e}")
        sys.exit(1)

if __name__ == "__main__":
    main()

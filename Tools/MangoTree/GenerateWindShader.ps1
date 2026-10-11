$ErrorActionPreference = 'Stop'
$project = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$package = Join-Path $project 'Library/PackageCache/com.unity.render-pipelines.universal@a6e30d1f59f5'
$output = Join-Path $project 'Assets/SecretsThatBreathe/Environment/MangoTree/Shaders'
$shader = Get-Content (Join-Path $package 'Shaders/Lit.shader') -Raw
$shader = $shader.Replace('Shader "Universal Render Pipeline/Lit"', 'Shader "Secrets That Breathe/Mango Wind Lit"')
$shader = $shader.Replace('"IgnoreProjector" = "True"', '"IgnoreProjector" = "True"' + "`n            " + '"DisableBatching" = "True"')
$props = @'
        [HideInInspector] _MangoWind("Wind", Vector) = (0,0,0,0)
        [HideInInspector] _MangoPreviousWind("Previous wind", Vector) = (0,0,0,0)
        [HideInInspector] _MangoWindTime("Wind time", Vector) = (0,0,0,0)
        [HideInInspector] _MangoRootWS("Tree root", Vector) = (0,0,0,0)
        [HideInInspector] _MangoUpWS("Tree up", Vector) = (0,1,0,0)
        [HideInInspector] _MangoShape("Tree shape", Vector) = (.8,3.4,.08,.026)
        [HideInInspector] _MangoLeafFlutter("Leaf flutter", Float) = 0
'@
$shader = $shader.Replace('        // Specular vs Metallic workflow', $props + "`n        // Specular vs Metallic workflow")
$passes = @(
    @{file='LitForwardPass.hlsl';entry='LitPassVertex';pos='positionOS';normal='normalOS';surface=$true;uv='texcoord'},
    @{file='LitGBufferPass.hlsl';entry='LitGBufferPassVertex';pos='positionOS';normal='normalOS';surface=$true;uv='texcoord'},
    @{file='ShadowCasterPass.hlsl';entry='ShadowPassVertex';pos='positionOS';surface=$false;uv='texcoord'},
    @{file='DepthOnlyPass.hlsl';entry='DepthOnlyVertex';pos='position';surface=$false;uv='texcoord'},
    @{file='LitDepthNormalsPass.hlsl';entry='DepthNormalsVertex';pos='positionOS';normal='normal';surface=$true;uv='texcoord'},
    @{file='Utils/Universal2D.hlsl';entry='vert';pos='positionOS';surface=$false;uv='uv'}
)
foreach ($pass in $passes) {
    $shader = $shader.Replace("#pragma vertex $($pass.entry)`r`n", "#pragma vertex Mango$($pass.entry)`r`n")
    $shader = $shader.Replace("#pragma vertex $($pass.entry)`n", "#pragma vertex Mango$($pass.entry)`n")
    $include = '#include "Packages/com.unity.render-pipelines.universal/Shaders/' + $pass.file + '"'
    $deform = if ($pass.surface) { "MangoDeformSurface(input.$($pass.pos).xyz, input.$($pass.normal), input.tangentOS, input.$($pass.uv));" } else { "input.$($pass.pos).xyz = MangoPositionOS(input.$($pass.pos).xyz, input.$($pass.uv), _MangoWindTime.x, _MangoWind);" }
    $wrapper = @"
$include
            #include "MangoWind.hlsl"
            Varyings Mango$($pass.entry)(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                $deform
                return $($pass.entry)(input);
            }
"@
    $shader = $shader.Replace($include, $wrapper)
}
$shader = $shader.Replace('Packages/com.unity.render-pipelines.universal/ShaderLibrary/ObjectMotionVectors.hlsl','MangoMotionVectors.hlsl')
$header = "// Adapted from Unity URP 17.3.0 Lit.shader. See Unity-LICENSE.md.`n// Shared deformation for forward, deferred, depth, normals, shadows and motion vectors.`n"
Set-Content -Encoding utf8 (Join-Path $output 'MangoWindLit.shader') ($header + $shader)
$motion = Get-Content (Join-Path $package 'ShaderLibrary/ObjectMotionVectors.hlsl') -Raw
$motion = $motion.Replace('#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MotionVectorsCommon.hlsl"', '#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MotionVectorsCommon.hlsl"' + "`n" + '#include "MangoWind.hlsl"')
$motion = $motion.Replace("#if _ALPHATEST_ON`r`n    float2 uv                   : TEXCOORD0;`r`n#endif", '    float2 uv                   : TEXCOORD0;')
$motion = $motion.Replace("#if _ALPHATEST_ON`n    float2 uv                   : TEXCOORD0;`n#endif", '    float2 uv                   : TEXCOORD0;')
$motion = $motion.Replace('    const VertexPositionInputs vertexInput', "    float3 undeformedPosition = input.position.xyz;`n    input.position.xyz = MangoPositionOS(undeformedPosition, input.uv, _MangoWindTime.x, _MangoWind);`n    const VertexPositionInputs vertexInput")
$motion = $motion.Replace('float4 prevPos = (unity_MotionVectorsParams.x == 1) ? float4(input.positionOld, 1) : input.position;', 'float4 prevPos = float4(MangoPositionOS(undeformedPosition, input.uv, _MangoWindTime.y, _MangoPreviousWind), 1);')
Set-Content -Encoding utf8 (Join-Path $output 'MangoMotionVectors.hlsl') ($header + $motion)
Copy-Item (Join-Path $package 'LICENSE.md') (Join-Path $output 'Unity-LICENSE.md') -Force

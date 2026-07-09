using UnityEditor;
using UnityEngine;

/// <summary>
/// Assets/Audio/ 하위로 임포트되는 모든 오디오 파일에 모바일(Android ARM64) 최적
/// 임포트 설정을 일괄 적용한다. CC0 팩 통째 임포트(D-02, ~130파일)의 수작업을 대체.
/// 반드시 오디오 팩 복사 이전에 이 스크립트가 커밋/컴파일되어 있어야 한다 (Pitfall 5).
/// </summary>
public class AudioImportSettings : AssetPostprocessor
{
    private void OnPreprocessAudio()
    {
        if (!assetPath.StartsWith("Assets/Audio/")) return;

        var importer = (AudioImporter)assetImporter;
        importer.forceToMono = true;               // 2D 게임 SFX — 메모리 50% 절감

        var settings = importer.defaultSampleSettings;
        settings.loadType          = AudioClipLoadType.DecompressOnLoad; // 짧은 SFX — 재생 시 디코딩 CPU 0
        settings.compressionFormat = AudioCompressionFormat.ADPCM;       // 임팩트성 SFX 모바일 표준 (~3.5:1)
        settings.sampleRateSetting = AudioSampleRateSetting.OptimizeSampleRate;
        importer.defaultSampleSettings = settings;
    }
}

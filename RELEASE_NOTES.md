# ChillWithYou-Mods — 릴리즈 노트

제목: v1.0.2 — 한글 패치 + Yt Downloader

요약
- 한글 번역 패치(Chill_with_You_Lo-Fi_Story_kr_v3.zip)와 Yt Downloader 유틸(YtDownloader.zip)을 함께 배포합니다.
- 압축을 풀면 나오는 항목을 게임이 설치된 폴더(게임 실행파일(.exe) 있는 곳)에 그대로 병합하면 됩니다.
- 이 GitHub 레포는 BepInEx 핵심 파일들을 일부러 제외하고 있으므로, 레포를 clone/pull해서 바로 사용하는 경우에는 별도 BepInEx 설치가 필요합니다. (릴리즈 ZIP은 필요한 파일을 포함하므로 압축을 풀어 바로 적용 가능)

포함된 파일(릴리즈 자산)
- Chill_with_You_Lo-Fi_Story_kr_v3.zip — 한글 번역/플러그인/설정/폰트 등 (게임 폴더에 병합하여 사용)
  - SHA256: 87b36d1177b4ec2b77bc5ec74538350a9f780c2cdfda74cfacfdec9490e24c26
- YtDownloader.zip — Yt Downloader 유틸 (자세한 사용법은 압축 내부 README 참조)
  - SHA256: sha256sum YtDownloader.zip

설치 요약 (압축 사용 시)
1. (권장) 게임 폴더 백업
2. 릴리즈 ZIP 다운로드 후 압축 해제
3. 압축에서 나온 항목을 게임 설치 폴더에 그대로 복사(병합)
   - 예: 압축 내부의 `BepInEx` 폴더는 게임 폴더의 `BepInEx`와 병합됩니다.
4. 게임 실행 후 `BepInEx/LogOutput.log` 또는 `BepInEx/logs/`에서 플러그인 로드 여부 확인

레포를 clone/pull로 사용하려는 경우 주의
- 레포는 BepInEx 코어(.dll 등)를 일부러 모두 포함하지 않습니다. 레포를 그대로 게임 폴더로 복사하면 플러그인이 동작하지 않을 수 있으니, 먼저 BepInEx 코어를 공식 페이지에서 받아 설치하세요:
  https://github.com/BepInEx/BepInEx/releases

변경사항 (간단)
- 한글: 메뉴/대사/설정 번역 추가 및 일부 오타 수정
- 유틸: Yt Downloader 추가(자세한 변경사항은 각 자산 내부 README 참조)

문제 발생 시
- BepInEx 설치 여부 확인
- `BepInEx/plugins`에 필요한 DLL이 있는지 확인
- 로그 파일(`BepInEx/LogOutput.log`)에서 에러 확인

문의/이슈
- 문제 발견 시 Issues에 남겨주세요: https://github.com/Ich-mag-dich/ChillWithYou-Mods/issues

감사합니다.
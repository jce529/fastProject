# /gsd:plan-vague

<objective>
확정된 기능 리스트를 바탕으로, 가장 빠른 시간 내에 코드를 작성할 수 있도록 원자적 단위의 작업 목록을 작성합니다.
</objective>

<process>
1. **작업 쪼개기**: 기능을 가장 작은 단위(Atomic Tasks)로 나눕니다.
2. **구현 체크리스트 작성**: `.planning/phases/Phase_N.md` 파일을 생성하여 바로 체크할 수 있는 리스트를 만듭니다.
   - [ ] [기능명] 기본 로직 구현
   - [ ] [기능명] 입력값 연결
   - [ ] [기능명] 동작 테스트 및 디버깅
3. **로드맵 업데이트**: `.planning/ROADMAP.md`에 현재 진행 상황과 다음 단계를 기록합니다.
4. **실행 지시**: "계획이 끝났습니다. 바로 `/gsd:execute-gemini 1`을 실행하십시오."

*주의: 이 커맨드는 **Claude 3 Opus**에서 실행하십시오.*
</process>

<success_criteria>
- 즉시 코딩이 가능한 상세 체크리스트 생성.
- 작업 간의 의존성이 명확히 정리됨.
</success_criteria>

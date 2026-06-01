# /gsd:execute-gemini [Phase_Number]

<objective>
수립된 계획에 따라 지체 없이 코드를 작성하고, 매 단계 진행 상황을 기록하여 끊김 없는 구현을 수행합니다.
</objective>

<process>
1. **태스크 읽기**: `.planning/phases/Phase_N.md`에서 수행할 다음 태스크를 확인합니다.
2. **코드 작성**: 해당 기능을 즉시 코드로 구현합니다. (아키텍처 고민보다 기능 완성 우선)
3. **진행 상태 기록 (Tracking)**:
   - 태스크 완료 시 `[x]` 표시.
   - `.planning/GEMINI_PROGRESS.md`에 작업 완료 기록 및 다음 작업 명시.
4. **커밋**: 작업 단위로 커밋하여 안정성을 확보합니다.
5. **반복**: 모든 태스크가 완료될 때까지 반복합니다.

*주의: 이 커맨드는 실행력이 좋은 **Gemini 3.1 Pro**에서 실행하십시오.*
</process>

<success_criteria>
- 계획된 기능이 코드로 모두 구현됨.
- 실시간 상태 기록을 통해 작업의 연속성 보장.
</success_criteria>

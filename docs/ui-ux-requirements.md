# Windows Desktop UI and UX Requirements for AI Developers

Version 1.0.0. Source review date: 2026-11-09. Language: English.

General desktop applications on supported Windows versions, independent of product, programming language, UI framework or component vendor. Use this profile as a reusable requirements baseline for an AI developer. The goal is predictable Windows interaction, usable task flows, accessible controls and checkable delivery.

## Adoption and interpretation

- MUST is mandatory when this profile is adopted. SHOULD is the default recommendation; departures require a reason and an alternative. These keywords express this profile's policy, not a claim that every item is a quoted external standard.
- Source-linked requirements are adaptations. Engineering-policy requirements add practical delivery rules. Source references identify relevant guidance, not exact normative equivalence.
- Apply requirements to the changed surface and affected end-to-end workflows. Broader audits require an explicit task.
- Use explicit task requirements and documented product decisions to resolve scope; retain applicable platform constraints and selected accessibility targets. Record conflicts and resolve consequential ambiguity before dependent implementation.
- Translate framework-specific examples into the actual stack. This profile does not mandate framework migration, a particular component vendor or a particular visual effect.
- International standards, platform guidance, web standards and informative adaptations have different authority and scope. A formal compliance claim requires its own applicable clauses and evidence.
- Selected accessibility checks here are not a complete WCAG, WCAG2ICT, ISO or legal compliance assessment. For formal conformance, add a complete applicability mapping and audit.
- Retrieve full ISO text when clause-level requirements or a conformance claim are needed; linked catalog abstracts support identification and scope only.
- Pin source/framework versions for a delivery. Recheck changing platform or vendor APIs when implementation depends on them.

## Instructions for the AI developer

1. Read the task and the adopted product profile, then identify applicable requirement IDs.
2. Inspect the actual framework, existing user conventions and relevant user entry points.
3. Record reasonable assumptions and continue independent reversible work. Ask only when a missing decision blocks a consequential choice.
4. Choose observable behavior before implementation details; preserve unrelated supported workflows.
5. Implement and verify the affected normal, failure and boundary cases.
6. Report implementation status separately from acceptance status. Cite evidence and leave limitations visible.
7. Follow the host environment's permissions for external actions and real input control. This document grants no additional permissions.

## Product configuration

Fill applicable required fields before relying on acceptance results. Proposed values below are starting points from this profile; product choices and measurement conditions determine release criteria. An unknown value is not evidence of a passing check.

| Key | Required | Initial value | Selection guidance |
| --- | --- | --- | --- |
| supported_windows_versions | Yes, when applicable | Not specified | Declare supported editions/build families; do not infer support from the development machine. |
| ui_framework_and_component_versions | Yes, when applicable | Not specified | Declare actual stack and relevant dependency versions; use matching documentation. |
| critical_user_tasks | Yes, when applicable | Not specified | List users, starting states, task outcomes and representative workloads. |
| minimum_client_size_dip | Yes, when applicable | Not specified | Select a usable client area in device-independent units. Distinguish it from physical display resolution. |
| display_scale_percent | No; suggested baseline | [100,125,150,200] | Suggested baseline matrix from this profile, not a universal Microsoft minimum. Extend for supported configurations. |
| text_scale_percent | No; suggested baseline | [100,200] | Test separately from display scaling; use an equivalent text-enlargement mechanism where needed. |
| themes | No; suggested baseline | ["light","dark","Windows contrast theme"] | Verify declared light/dark support and usable Windows contrast behavior; document supported combinations. |
| locales_and_input_modes | Yes, when applicable | Not specified | Declare supported languages, regional formats, keyboard/IME, pointer and optional touch/pen. |
| interaction_feedback_target_ms | No; suggested baseline | 100 | Proposed initial feedback target; select the actual budget and measurement method per workflow. Not a sourced universal standard. |
| progress_display_threshold_ms | No; suggested baseline | 1000 | Proposed initial threshold for sustained work; tune to task and avoid flicker. Not a universal standard. |
| cancellation_budgets | Yes, when applicable | Not specified | For cancelable work, specify request acknowledgement and safe-stop budgets per phase/workload; identify indivisible work. |
| usability_measures | Yes, when applicable | Not specified | Select task-completion, error, effort and satisfaction measures appropriate to the intended users. |

DIP means device-independent pixel or the equivalent logical unit in the chosen framework. Display scaling, text scaling and document/content zoom are separate test dimensions.

## Requirements

Each acceptance check is scoped to the declared supported configurations. A source reference identifies related guidance; `engineering_policy` identifies an original delivery rule adopted by this profile.

### User workflows

**UX-001 Define the user task**

- Strength: MUST.
- Applies to: Every new or materially changed workflow.
- Required behavior: Identify the intended users, task, starting state, completion outcome and relevant usage constraints before choosing UI behavior.
- Acceptance: Provide at least one observable success scenario and a realistic failure or boundary scenario.
- Basis: profile_adaptation. Sources: [S02], [S03], [S04].

**UX-002 Preserve user context**

- Strength: MUST.
- Applies to: Navigation, refresh, filtering and modal or modeless tools.
- Required behavior: Preserve relevant input, selection, scroll position and working context unless the requested action deliberately replaces them.
- Acceptance: Perform a context-changing action and return; verify retained values and the documented exceptions.
- Basis: engineering_policy.

### Controls and visual design

**UI-001 Use conventional controls**

- Strength: MUST.
- Applies to: All interactive surfaces.
- Required behavior: Prefer supported framework or operating-system controls and established behavior. Custom controls must implement equivalent interaction and accessibility for their intended function.
- Acceptance: Check equivalent commands across affected surfaces and inspect any custom-control behavior.
- Basis: profile_adaptation. Sources: [S01].

**UI-002 Use consistent visual tokens**

- Strength: MUST.
- Applies to: All affected screens.
- Required behavior: Use a coherent set of typography, spacing, color and icon tokens. Size controls for their content and group related controls clearly.
- Acceptance: Compare repeated elements and inspect normal, focused, selected, disabled and error states.
- Basis: profile_adaptation. Sources: [S12], [S13].

**UI-003 Make commands understandable**

- Strength: MUST.
- Applies to: Commands, icons and labels.
- Required behavior: Use consistent task-oriented terminology and specific action labels. Essential instructions must be available without relying solely on a tooltip or hover.
- Acceptance: Verify users can identify the primary action and required input using visible labels and keyboard focus.
- Basis: profile_adaptation. Sources: [S10], [S07].

### Layout and scaling

**LAY-001 Keep layouts usable**

- Strength: MUST.
- Applies to: All windows and panels.
- Required behavior: Keep essential content and commands reachable at the supported minimum client size. Use adaptive layout, wrapping or scrolling without overlap or loss of functionality.
- Acceptance: Check minimum, typical and maximized sizes with long labels and representative large values.
- Basis: profile_adaptation. Sources: [S01], [S12].

**LAY-002 Handle display DPI changes**

- Strength: MUST.
- Applies to: Applications used on supported multi-DPI Windows configurations.
- Required behavior: Use framework-appropriate logical sizing and DPI handling. Recalculate layout and rendering when the display scale changes; do not use physical pixels as universal UI dimensions.
- Acceptance: Move a window between different-DPI monitors and test a supported runtime scale change; inspect text, hit areas, popups and dialogs.
- Basis: profile_adaptation. Sources: [S14].

**LAY-003 Support readable text scaling**

- Strength: MUST.
- Applies to: Text-based UI.
- Required behavior: Preserve content and function with text enlarged to 200 percent using supported platform settings or an equivalent application mechanism. Evaluate essential two-dimensional surfaces separately and document their alternatives.
- Acceptance: Increase text independently of display DPI where supported; check labels, input, validation and command reachability.
- Basis: profile_adaptation. Sources: [S08], [S09], [S13].

### Windows and dialogs

**WIN-001 Keep windows reachable**

- Strength: MUST.
- Applies to: Top-level windows, dialogs and popups.
- Required behavior: Open owned dialogs with the correct owner and usable placement. Restore saved bounds into a current monitor work area and preserve a reachable title bar or equivalent move/close mechanism.
- Acceptance: Disconnect a previously used monitor and reopen the window; verify ownership, placement and keyboard dismissal.
- Basis: profile_adaptation. Sources: [S16].

**WIN-002 Use deliberate modality**

- Strength: MUST.
- Applies to: Dialogs and auxiliary tools.
- Required behavior: Use modal interaction when a decision must block the affected workflow. Use modeless tools when users need continued interaction with the underlying content. Background completion must not unexpectedly steal focus.
- Acceptance: Verify blocked and available windows, minimize/restore behavior and focus after completion.
- Basis: profile_adaptation. Sources: [S10], [S16].

**WIN-003 Provide clear and safe dialog actions**

- Strength: MUST.
- Applies to: Confirmation and action dialogs.
- Required behavior: Use concise action-specific labels and a safe dismissal path. Prevent accidental destructive activation through initial focus, default-button behavior or an unrelated Enter key.
- Acceptance: Check mouse, Enter, Escape and title-bar close; verify the destructive action requires the intended user action.
- Basis: profile_adaptation. Sources: [S10].

### Input and focus

**INP-001 Provide keyboard access**

- Strength: MUST.
- Applies to: All functionality except operations inherently dependent on a free movement path.
- Required behavior: Provide keyboard access to commands and workflows, including menus and composite controls. Provide a practical alternative for essential pointer-specific interactions where feasible.
- Acceptance: Complete each critical workflow using the keyboard; document narrowly scoped inherent-input exceptions.
- Basis: profile_adaptation. Sources: [S06], [S08], [S09].

**INP-002 Manage focus predictably**

- Strength: MUST.
- Applies to: Every interactive surface.
- Required behavior: Use logical initial and traversal focus, visible focus indication and appropriate restoration after temporary UI closes. Confine focus inside modal dialogs only while their modality applies.
- Acceptance: Traverse forward/backward, enter and leave composite controls, close dialogs and verify no unintended focus trap or obscured focused control.
- Basis: profile_adaptation. Sources: [S06].

**INP-003 Respect key context**

- Strength: MUST.
- Applies to: Text fields, editors, lists and command surfaces.
- Required behavior: Apply Enter, Escape, Space and arrow keys according to the focused control. Preserve normal text editing and IME composition before handling global shortcuts.
- Acceptance: Type and compose text, edit a grid cell and open a popup while command shortcuts are enabled; verify no unintended action.
- Basis: profile_adaptation. Sources: [S05].

**INP-004 Keep pointer targets usable**

- Strength: MUST.
- Applies to: Pointer-operated controls and touch surfaces when touch is in scope.
- Required behavior: Keep actionable areas distinguishable and usable at supported scaling and input modes. Provide an alternative to essential drag-only commands when feasible. Do not apply CSS-pixel target sizes to native controls without an explicit mapping.
- Acceptance: Check pointer activation near target edges and test the supported alternative to dragging; include touch only when in scope.
- Basis: profile_adaptation. Sources: [S08], [S09].

### Forms and validation

**FORM-001 Label input and explain constraints**

- Strength: MUST.
- Applies to: Input forms and editable controls.
- Required behavior: Associate persistent accessible labels with inputs. Identify required fields, units, accepted formats and relevant limits before submission.
- Acceptance: Inspect visible and accessible labels and submit representative empty, boundary and incorrectly formatted values.
- Basis: profile_adaptation. Sources: [S07], [S08], [S09].

**FORM-002 Support correction without data loss**

- Strength: MUST.
- Applies to: Validation failures.
- Required behavior: Place actionable validation near the affected input, preserve other valid input and provide an accessible way to discover the error. Do not rely on color alone.
- Acceptance: Submit invalid input, navigate to the error with a keyboard or screen reader, correct it and complete the action.
- Basis: profile_adaptation. Sources: [S10], [S17].

### State and data

**STATE-001 Define settings commit behavior**

- Strength: MUST.
- Applies to: Settings and property editors.
- Required behavior: Define commit boundaries for OK, Apply, Cancel and title-bar close. In staged editors, Cancel discards changes since the last successful Apply; it does not undo already committed changes. Immediate-apply settings must make their behavior clear.
- Acceptance: Check edit/Cancel, edit/OK, Apply/edit/Cancel and commit-failure paths against reopened values.
- Basis: engineering_policy.

**STATE-002 Protect user work**

- Strength: MUST.
- Applies to: Unsaved documents, destructive actions and replacement operations.
- Required behavior: Use an appropriate prevention, confirmation, undo or recovery mechanism for meaningful loss of user work. Never report save success while the result is failed or unknown.
- Acceptance: Exercise unsaved close, save failure and destructive-action recovery; reopen persisted output when relevant.
- Basis: engineering_policy.

**STATE-003 Declare persistence scope**

- Strength: MUST.
- Applies to: Preferences, session state and document state.
- Required behavior: Define which state is transient, per document, per user or shared. Restore it consistently, handle incompatible stored values and avoid silently changing unrelated settings.
- Acceptance: Save/reopen and restart; inspect scope boundaries, invalid stored values and shared-state conflicts where applicable.
- Basis: engineering_policy.

### Lists and data views

**DATA-001 Keep selection and operations aligned**

- Strength: MUST.
- Applies to: Lists, tables, trees and filtered or virtualized data views.
- Required behavior: Keep focus, selection and operation scope distinguishable. Sorting, filtering and refresh must preserve logical item identity or deliberately clear selection; batch commands must use the displayed or explicitly declared scope.
- Acceptance: Sort/filter/refresh a selected view and run a batch action; verify the exact items affected, including offscreen items.
- Basis: engineering_policy.

**DATA-002 Represent empty and unavailable data**

- Strength: MUST.
- Applies to: Data-driven surfaces.
- Required behavior: Distinguish no data, no matching results, loading, access failure and unavailable/offline states. Offer relevant recovery without representing an error as an empty successful result.
- Acceptance: Exercise each applicable state and verify its message and available actions.
- Basis: engineering_policy.

### Long operations

**ASYNC-001 Keep interaction responsive**

- Strength: MUST.
- Applies to: Work that may block interaction.
- Required behavior: Keep the UI message loop responsive and prevent conflicting reentry. Allow unrelated interaction when safe; if content must be blocked, continue painting and handling permitted actions.
- Acceptance: Run the agreed large/slow workload and test repaint, window movement and available commands while work is active.
- Basis: profile_adaptation. Sources: [S01].

**ASYNC-002 Show truthful progress**

- Strength: MUST.
- Applies to: Operations exceeding the selected progress-display threshold.
- Required behavior: Show operation state and useful phase information. Use a percentage only when progress can be meaningfully measured; otherwise show indeterminate progress. Do not present completion before the required work completes.
- Acceptance: Run success, slow and stalled cases; compare displayed state with actual operation phases.
- Basis: profile_adaptation. Sources: [S11].

**ASYNC-003 Implement honest cancellation**

- Strength: MUST.
- Applies to: Cancelable operations and operations with a commit boundary.
- Required behavior: Define when cancellation is accepted, how it is acknowledged and what state remains. Stop safely, preserve earlier user work and make an unavailable cancellation boundary clear.
- Acceptance: Cancel before and during work, near commit and after completion; verify persisted data and the displayed outcome.
- Basis: engineering_policy.

**ASYNC-004 Report distinct outcomes**

- Strength: MUST.
- Applies to: All operations with meaningful failure or cancellation.
- Required behavior: Distinguish success, cancellation, failure, partial completion and unknown outcome when those states can occur. Retry must not silently duplicate effects or overwrite newer work.
- Acceptance: Inject relevant failures and a repeated action; compare user messages, actual effects and retained work.
- Basis: engineering_policy.

### Messages and recovery

**MSG-001 Explain errors and next steps**

- Strength: MUST.
- Applies to: User-visible errors.
- Required behavior: Explain what failed and a useful next action in user terminology. Provide technical details separately when helpful, and preserve enough context to diagnose recurring failures.
- Acceptance: Trigger a known failure and verify correction/retry or an appropriate support path without losing entered data.
- Basis: profile_adaptation. Sources: [S17].

**MSG-002 Avoid unnecessary interruption**

- Strength: MUST.
- Applies to: Notifications and confirmations.
- Required behavior: Use nonmodal feedback for routine outcomes. Reserve interrupting messages for information or decisions needed to continue safely.
- Acceptance: Complete normal and repeated actions; verify feedback is discoverable and does not unnecessarily capture focus.
- Basis: profile_adaptation. Sources: [S10].

### Accessibility

**A11Y-001 Expose accessible semantics**

- Strength: MUST.
- Applies to: Interactive controls, status and meaningful content.
- Required behavior: Expose accurate names, roles, states, values and supported actions through the relevant Windows accessibility interface, normally UI Automation. Custom controls must provide equivalent semantics.
- Acceptance: Inspect the accessibility tree and run a critical workflow with Narrator, including state changes and validation.
- Basis: profile_adaptation. Sources: [S07].

**A11Y-002 Meet contrast and state visibility targets**

- Strength: MUST.
- Applies to: UI text and meaningful control or graphical information.
- Required behavior: Adopt WCAG 2.2 criteria 1.4.3, 1.4.11 and 1.4.1 as interpreted for software by WCAG2ICT: ordinary text 4.5:1, qualifying large text 3:1, and applicable non-text information 3:1. Preserve the criteria's exceptions and do not use color alone.
- Acceptance: Measure representative required states in supported themes; identify any relied-on criterion exception and test contrast themes.
- Basis: profile_adaptation. Sources: [S08], [S09].

**A11Y-003 Respect platform preferences**

- Strength: MUST.
- Applies to: Themes, animation and assistive technologies.
- Required behavior: Support Windows contrast preferences and avoid overriding user settings that affect legibility or interaction. Where animation is optional, honor reduced-motion preferences through the framework's supported mechanism.
- Acceptance: Change relevant system preferences and verify controls, focus and essential information remain usable.
- Basis: profile_adaptation. Sources: [S01], [S07].

### Language and content

**LOC-001 Support declared locales**

- Strength: MUST.
- Applies to: Applications supporting multiple languages or regional formats.
- Required behavior: Separate localizable UI text from code, allow text expansion and use declared locale rules for display and parsing. Make units and ambiguous dates explicit; support bidirectional layout where required.
- Acceptance: Test long translated strings, locale-specific numeric/date input, keyboard composition and bidirectional layout when in scope.
- Basis: engineering_policy.

### User workflows

**UX-003 Measure task usability**

- Strength: SHOULD.
- Applies to: New workflows and material redesigns.
- Required behavior: Evaluate representative user tasks against selected completion, error and effort measures. Automated UI checks supplement evaluation with representative users; they do not establish usability by themselves.
- Acceptance: Record the task, participants/context, measures and observed difficulties for the evaluated scope.
- Basis: profile_adaptation. Sources: [S03], [S04].

### Verification

**QA-001 Verify the affected workflow**

- Strength: MUST.
- Applies to: Every delivered UI change.
- Required behavior: Check the actual user entry point and relevant normal, failure and boundary paths. Choose automated, visual, accessibility and user evaluation methods according to the changed risk.
- Acceptance: Provide an acceptance result for each applicable requirement, with build/environment and evidence; leave unperformed checks explicit.
- Basis: engineering_policy.

**QA-002 Scope claims to evidence**

- Strength: MUST.
- Applies to: Completion and acceptance reports.
- Required behavior: Use Pass, Fail, Not run, Blocked or Not applicable. Pass requires an executed acceptance check. Code review and implementation status must be reported separately; unavailable evidence cannot be labeled Pass.
- Acceptance: Audit the final acceptance record for reproducible conditions, specific reasons and evidence links.
- Basis: engineering_policy.

## Verification matrix

Select combinations relevant to the changed behavior rather than claiming an unexecuted Cartesian matrix. State exactly which combinations ran.

| ID | Scope | Conditions | Method | Requirements |
| --- | --- | --- | --- | --- |
| VM-01 | Affected screens | Minimum, typical and maximized client areas; representative long content | Visual and layout checks | LAY-001, UI-002, UI-003 |
| VM-02 | Windows, dialogs and popups | Configured display scales; mixed-DPI move; disconnected monitor | Runtime rendering, placement and hit-area checks | LAY-002, WIN-001 |
| VM-03 | Text and themes | Independent text enlargement; declared themes and Windows contrast theme | Visual inspection plus contrast measurement | LAY-003, A11Y-002, A11Y-003 |
| VM-04 | Critical workflows | Keyboard only; visible focus; editable controls and IME where supported | End-to-end interaction | INP-001, INP-002, INP-003 |
| VM-05 | Controls, status and errors | Narrator and accessibility-tree inspection | Inspect names, roles, values, patterns and announced changes | A11Y-001, FORM-001, FORM-002 |
| VM-06 | Settings and persisted data | Apply/Cancel/OK/close; save/reopen; invalid stored values | Inspect actual restored and persisted state | STATE-001, STATE-002, STATE-003 |
| VM-07 | Long or remote operations | Representative large/slow workload; failure; cancellation near commit; retry | Observe responsiveness, data effects and truthful outcome | ASYNC-001, ASYNC-002, ASYNC-003, ASYNC-004 |
| VM-08 | Data views and locales | Sort/filter/refresh; offscreen selection; declared regional formats | Verify identity, action scope and input/output | DATA-001, DATA-002, LOC-001 |
| VM-09 | New or materially redesigned tasks | Representative users and realistic task conditions | Measure selected usability outcomes and record difficulties | UX-001, UX-003 |

## Acceptance records

Maintain one record per applicable requirement and verification scenario. Use the following fields:

- `requirement_id`
- `applicability`
- `implementation_status`
- `acceptance_status`
- `build_revision`
- `environment`
- `scenario_and_data`
- `expected_result`
- `observed_result`
- `evidence`
- `limitation_or_exception`
- `next_action_and_owner_when_needed`

| Acceptance status | Meaning |
| --- | --- |
| Pass | Applicable acceptance check executed successfully with evidence. |
| Fail | Executed check did not meet the requirement. |
| Not run | Applicable check was not executed. |
| Blocked | Check could not proceed; name the condition that enables it. |
| Not applicable | Requirement does not apply; provide a specific reason. |

Implementation status such as Planned or Implemented is separate from acceptance status. Visual screenshots cannot establish persisted data, cancellation correctness or assistive-technology behavior on their own.

### Example acceptance record

```json
{
  "requirement_id": "STATE-001",
  "applicability": "Applies to a staged Preferences dialog.",
  "implementation_status": "Implemented",
  "acceptance_status": "Not run",
  "build_revision": null,
  "environment": null,
  "scenario_and_data": "Set value A; Apply; change to B; Cancel; reopen Preferences.",
  "expected_result": "Value A remains committed; B is discarded; unrelated settings remain unchanged.",
  "observed_result": null,
  "evidence": [],
  "limitation_or_exception": "Execution and persisted-state evidence are pending.",
  "next_action_and_owner_when_needed": "Run the scenario on a named build; owner to be assigned by the product team."
}
```

## Definition of done

- Applicable MUST requirements have passing acceptance checks for the delivered scope.
- SHOULD departures and applicability exclusions have specific reasons and alternatives where relevant.
- Unrun or blocked MUST checks remain explicit acceptance gaps; do not call the affected scope fully verified.
- Results identify build/revision, environment, scenario/data, expected versus observed behavior and evidence.
- Known failures affecting the changed behavior are investigated; matching an old test name does not prove an unchanged cause.
- Normal, failure and relevant boundary cases preserve intended user work and truthful outcomes.
- Source and configuration references are retained so the decision can be reviewed without the original conversation.

## Sources and scope

- **S01 [Windows app development best practices](https://learn.microsoft.com/en-us/windows/apps/get-started/best-practices)**. Classification: platform_guidance. General Windows behavior and integration. Adapt framework-specific examples to the chosen stack.
- **S02 [ISO 9241-110:2020 Interaction principles](https://www.iso.org/standard/75258.html)**. Classification: international_standard. Conceptual basis for interaction requirements. The catalog abstract does not provide clause-level compliance evidence.
- **S03 [ISO 9241-11:2018 Usability](https://www.iso.org/standard/63500.html)**. Classification: international_standard. Usability concepts and context of use. Product-specific success measures must be defined.
- **S04 [ISO 9241-210:2019 Human-centred design](https://www.iso.org/standard/77520.html)**. Classification: international_standard. Basis for understanding users, evaluating designs and iterating. Consult the full standard for clause-level obligations.
- **S05 [Keyboard interactions](https://learn.microsoft.com/en-us/windows/apps/develop/input/keyboard-interactions)**. Classification: platform_guidance. Keyboard conventions and context-sensitive key handling; WinUI API examples are not framework mandates.
- **S06 [Keyboard accessibility](https://learn.microsoft.com/en-us/windows/apps/design/accessibility/keyboard-accessibility)**. Classification: platform_guidance. Keyboard navigation, logical focus order and composite controls.
- **S07 [Accessibility checklist](https://learn.microsoft.com/en-us/windows/apps/design/accessibility/accessibility-checklist)**. Classification: platform_guidance. Accessible names, UI Automation, contrast, keyboard and assistive technology checks.
- **S08 [WCAG 2.2](https://www.w3.org/TR/WCAG22/)**. Classification: web_standard. Normative for web content. This profile adopts selected criteria for native software using S09; it is not a complete conformance checklist.
- **S09 [WCAG2ICT](https://www.w3.org/TR/wcag2ict/)**. Classification: informative_guidance. Informative application of WCAG to non-web documents and software; does not itself establish normative requirements.
- **S10 [Dialog controls](https://learn.microsoft.com/en-us/windows/apps/develop/ui/controls/dialogs-and-flyouts/dialogs)**. Classification: platform_guidance. Dialog purpose, actionable wording and safe dismissal; adapt WinUI APIs to the actual framework.
- **S11 [Progress controls](https://learn.microsoft.com/en-us/windows/apps/develop/ui/controls/progress-controls)**. Classification: platform_guidance. Progress presentation and determinate versus indeterminate states.
- **S12 [Content layout and spacing](https://learn.microsoft.com/en-us/windows/apps/design/basics/content-basics)**. Classification: platform_guidance. Consistent grouping and spacing. Effective pixels are not physical screen pixels.
- **S13 [Typography in Windows](https://learn.microsoft.com/en-us/windows/apps/design/signature-experiences/typography)**. Classification: platform_guidance. Readable hierarchy, Windows typography and logical sizing; preserve appropriate framework defaults.
- **S14 [High DPI desktop application development](https://learn.microsoft.com/en-us/windows/win32/hidpi/high-dpi-desktop-application-development-on-windows)**. Classification: platform_guidance. DPI changes and multiple monitors. Its older framework-selection advice is not adopted by this profile.
- **S15 [Windows User Experience Interaction Guidelines](https://learn.microsoft.com/en-us/windows/win32/uxguide/guidelines)**. Classification: legacy_platform_guidance. Windows 7-era guidance. Use behavioral principles where still applicable, not as the default modern visual specification.
- **S16 [Window management](https://learn.microsoft.com/en-us/windows/win32/uxguide/win-window-mgt)**. Classification: legacy_platform_guidance. Ownership, placement and focus behavior, subject to current platform behavior.
- **S17 [Error messages](https://learn.microsoft.com/en-us/windows/win32/uxguide/mess-error)**. Classification: legacy_platform_guidance. Actionable error explanations and appropriate message placement; legacy visual examples are not adopted.

The companion JSON contains the same profile, requirements, configuration, verification matrix, source registry and example in structured form. Keep versions synchronized when updating either representation.

[S01]: https://learn.microsoft.com/en-us/windows/apps/get-started/best-practices
[S02]: https://www.iso.org/standard/75258.html
[S03]: https://www.iso.org/standard/63500.html
[S04]: https://www.iso.org/standard/77520.html
[S05]: https://learn.microsoft.com/en-us/windows/apps/develop/input/keyboard-interactions
[S06]: https://learn.microsoft.com/en-us/windows/apps/design/accessibility/keyboard-accessibility
[S07]: https://learn.microsoft.com/en-us/windows/apps/design/accessibility/accessibility-checklist
[S08]: https://www.w3.org/TR/WCAG22/
[S09]: https://www.w3.org/TR/wcag2ict/
[S10]: https://learn.microsoft.com/en-us/windows/apps/develop/ui/controls/dialogs-and-flyouts/dialogs
[S11]: https://learn.microsoft.com/en-us/windows/apps/develop/ui/controls/progress-controls
[S12]: https://learn.microsoft.com/en-us/windows/apps/design/basics/content-basics
[S13]: https://learn.microsoft.com/en-us/windows/apps/design/signature-experiences/typography
[S14]: https://learn.microsoft.com/en-us/windows/win32/hidpi/high-dpi-desktop-application-development-on-windows
[S15]: https://learn.microsoft.com/en-us/windows/win32/uxguide/guidelines
[S16]: https://learn.microsoft.com/en-us/windows/win32/uxguide/win-window-mgt
[S17]: https://learn.microsoft.com/en-us/windows/win32/uxguide/mess-error

# Decision — The kit describes the limits mechanism; each project decides its policies

**Status:** accepted

**Context:** A product project built trust-boundary limits that the end user can see, understand, and change: one declaration per limit with its explanation, a hard range and `off`, a standard hit sentence, fixed defaults, and a catalog check. The result worked and the maintainer wants every project that uses the kit to follow it in C#, Rust, and Python. The product project also made policy decisions that are right for it and wrong for others: a reference machine, safety factors, which front end may change a limit, and `off` for every limit. Section 4.2 of [`copilot-instructions.md`](../../.github/copilot-instructions.md) said that neither posture wins by rule.

**Decision:** One skill, [`tech-limits.md`](../../.github/skills/tech-limits.md), loads when a trust-boundary limit is in scope (Section 3 trigger row).

- The kit describes the **mechanism**: what a conforming implementation offers. It states no number.
- The decisions are **slots of the project**, recorded in its `custom_instructions.md` or a decision record: posture, reference machine and workload, safety margin of defaults, front ends that may change a limit, `off` per limit, key and text style, home of the register. The skill lists each slot with the default that applies when the project chose nothing. Secure by default (active, finite, never tighter than the host handles) is the default posture (Section 4.2).
- The reference implementation for C# is the pair of packages `NetworkInspector.Limits` and `NetworkInspector.Settings`. Rust and Python have none yet; their skills say to implement the mechanism in their own idiom.
- The audit is part of `/review`: a limits hunt, and a full audit is a review whose scope names it. The register is a tracked document.

**Rejected:**
- Constants only: invisible and unchangeable, and a user whose legitimate input hits the bound has no way out.
- A long option or attribute per limit: each limit would invent its own spelling, help, and message.
- A warning zone between a recommended and an allowed range: two ranges and a state nobody can explain.
- Access control by origin as a kit rule: which front end may change which limit is a decision of each application; the bullet that keeps a ceiling out of end-user reach stays as a decision of the project.
- A `/limits-audit` workflow: the audit is a review hunt with a register as its result, so no stage structure of its own is needed.
- Numbers in the kit (reference machine, factors): they are right for one project and wrong for another, and every load would pay for them.
- The mechanism copied into each language skill: paraphrases drift; the language skills keep the idiom and point to the skill.
- Defaults derived from the machine (a share of memory or processors with a clamp, and a memory budget over all shares): hard to test and to explain, and a default that differs from machine to machine. A fixed number for a stated reference machine is enough.

**Consequences:** A project that used the kit with an open posture by default records that choice and its reason (Section 4.2 already required the reason). `/plan` loads the skill and fills the policy slots; `/review` reports a bare bound at a trust boundary as an Error. The skill and Section 4.2 hold no policy number, so a project's numbers live in one place per project.

**Enforced in:** [`tech-limits.md`](../../.github/skills/tech-limits.md) § Mechanism and § Decisions of the project; the trigger row in [`copilot-instructions.md`](../../.github/copilot-instructions.md) Section 3; the Limits hunt in [`workflow-review.md`](../../.github/skills/workflow-review.md).

# Examine text after when they're holding something (in-hand)

comp-hands-examine-wrapper = { INDEFINITE($item) } [color=Red]{$item}[/color]

hands-system-blocked-by = Blocked by

comp-hands-examine = { CAPITALIZE(SUBJECT($user)) } { CONJUGATE-BE($user) } holding { $items }.
comp-hands-examine-empty = { CAPITALIZE(SUBJECT($user)) } { CONJUGATE-BE($user) } not holding anything.

# self aware versions

comp-hands-examine-selfaware = You are holding { $items }.
comp-hands-examine-empty-selfaware = You are not holding anything.

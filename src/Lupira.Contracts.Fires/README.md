# Lupira.Contracts.Fires

Wire contract of the fire push from the calendar dispatcher to the assistant (`POST /fires`): `FireRequest` with its `ItemPrompt` or `ItemAction` payload, and `FireAcceptedResponse`. BCL only.

Every enum always serializes by name, regardless of the host's serializer options. Cal persists `ItemPrompt`/`ItemAction` inside its events and item documents, so member and enum names are a stored shape too: add members, never rename or remove them.

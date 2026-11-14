CREATE EXTENSION IF NOT EXISTS pgcrypto;
CREATE TABLE IF NOT EXISTS app_users (
 id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
 display_name VARCHAR(40) NOT NULL,
 created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
 updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);
CREATE TABLE IF NOT EXISTS card_catalog_versions (
 id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
 version VARCHAR(32) NOT NULL UNIQUE,
 content_hash CHAR(64) NOT NULL,
 published_at TIMESTAMPTZ,
 created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);
CREATE TABLE IF NOT EXISTS ruleset_versions (
 id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
 version VARCHAR(32) NOT NULL UNIQUE,
 definition JSONB NOT NULL,
 content_hash CHAR(64) NOT NULL,
 created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);
CREATE TABLE IF NOT EXISTS matches (
 id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
 status VARCHAR(24) NOT NULL,
 seed BIGINT NOT NULL,
 ruleset_version_id UUID NOT NULL REFERENCES ruleset_versions(id),
 card_catalog_version_id UUID NOT NULL REFERENCES card_catalog_versions(id),
 state_version BIGINT NOT NULL DEFAULT 0,
 started_at TIMESTAMPTZ,
 completed_at TIMESTAMPTZ,
 created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);
CREATE TABLE IF NOT EXISTS match_seats (
 id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
 match_id UUID NOT NULL REFERENCES matches(id) ON DELETE CASCADE,
 user_id UUID REFERENCES app_users(id),
 seat_number SMALLINT NOT NULL CHECK (seat_number BETWEEN 0 AND 3),
 controller_type VARCHAR(8) NOT NULL CHECK (controller_type IN ('human','cpu')),
 display_name VARCHAR(40) NOT NULL,
 result VARCHAR(16),
 UNIQUE(match_id, seat_number)
);
CREATE TABLE IF NOT EXISTS match_events (
 match_id UUID NOT NULL REFERENCES matches(id) ON DELETE CASCADE,
 sequence BIGINT NOT NULL,
 event_type VARCHAR(80) NOT NULL,
 payload JSONB NOT NULL,
 created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
 PRIMARY KEY(match_id, sequence)
);
CREATE TABLE IF NOT EXISTS match_snapshots (
 match_id UUID NOT NULL REFERENCES matches(id) ON DELETE CASCADE,
 state_version BIGINT NOT NULL,
 state JSONB NOT NULL,
 created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
 PRIMARY KEY(match_id, state_version)
);
CREATE INDEX IF NOT EXISTS ix_matches_status_created ON matches(status, created_at);
CREATE INDEX IF NOT EXISTS ix_match_events_created ON match_events(created_at);

#!/bin/bash
set -e

COSMOS_ENDPOINT="https://cosmos-emulator:8081"
COSMOS_KEY="C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw=="
DATABASE_NAME="BiotrackrDB"
SEED_DATA_DIR=".devcontainer/seed-data"

echo "Initializing Cosmos DB..."

# Wait for Cosmos emulator HTTPS endpoint
echo "Waiting for Cosmos DB HTTPS endpoint..."
until curl -k -f -s "${COSMOS_ENDPOINT}/" > /dev/null 2>&1; do
    sleep 2
done
echo "Cosmos DB HTTPS endpoint is ready."

# Generate Cosmos DB REST API auth header
# Args: verb, resourceType, resourceLink
generate_auth() {
    local verb="$1" resource_type="$2" resource_link="$3"
    local date
    date=$(date -u '+%a, %d %b %Y %H:%M:%S GMT')
    local payload
    payload="$(printf '%s\n%s\n%s\n%s\n%s\n' "${verb,,}" "${resource_type,,}" "$resource_link" "${date,,}" "")"
    local hex_key
    hex_key=$(echo -n "$COSMOS_KEY" | base64 -d | xxd -p -c 256)
    local signature
    signature=$(printf '%s' "$payload" | openssl dgst -sha256 -mac HMAC -macopt "hexkey:${hex_key}" -binary | base64)
    local encoded_sig
    encoded_sig=$(python3 -c "import urllib.parse; print(urllib.parse.quote('type=master&ver=1.0&sig=${signature}', safe=''))")
    AUTH_DATE="$date"
    AUTH_TOKEN="$encoded_sig"
}

# Create database
echo "Creating database '$DATABASE_NAME'..."
generate_auth "POST" "dbs" ""
HTTP_CODE=$(curl -k -s -o /dev/null -w "%{http_code}" \
    -X POST "${COSMOS_ENDPOINT}/dbs" \
    -H "Authorization: ${AUTH_TOKEN}" \
    -H "x-ms-date: ${AUTH_DATE}" \
    -H "x-ms-version: 2018-12-31" \
    -H "Content-Type: application/json" \
    -d "{\"id\": \"${DATABASE_NAME}\"}")

if [ "$HTTP_CODE" = "201" ]; then
    echo "  Database created."
elif [ "$HTTP_CODE" = "409" ]; then
    echo "  Database already exists."
else
    echo "  WARNING: Unexpected response $HTTP_CODE creating database."
fi

# Create container helper
# Args: containerName, partitionKey, [defaultTtl]
create_container() {
    local container_name="$1" partition_key="$2" default_ttl="$3"
    local resource_link="dbs/${DATABASE_NAME}"

    echo "Creating container '$container_name'..."
    generate_auth "POST" "colls" "$resource_link"

    local body="{\"id\": \"${container_name}\", \"partitionKey\": {\"paths\": [\"/${partition_key}\"], \"kind\": \"Hash\", \"version\": 2}"
    if [ -n "$default_ttl" ]; then
        body="${body}, \"defaultTtl\": ${default_ttl}}"
    else
        body="${body}}"
    fi

    HTTP_CODE=$(curl -k -s -o /dev/null -w "%{http_code}" \
        -X POST "${COSMOS_ENDPOINT}/dbs/${DATABASE_NAME}/colls" \
        -H "Authorization: ${AUTH_TOKEN}" \
        -H "x-ms-date: ${AUTH_DATE}" \
        -H "x-ms-version: 2018-12-31" \
        -H "x-ms-offer-throughput: 400" \
        -H "Content-Type: application/json" \
        -d "$body")

    if [ "$HTTP_CODE" = "201" ]; then
        echo "  Container '$container_name' created."
    elif [ "$HTTP_CODE" = "409" ]; then
        echo "  Container '$container_name' already exists."
    else
        echo "  WARNING: Unexpected response $HTTP_CODE creating container '$container_name'."
    fi
}

create_container "records" "documentType"
create_container "conversations" "sessionId" "7776000"

# Insert seed documents
insert_count=0
skip_count=0

insert_documents() {
    local container="$1" partition_key_field="$2" json_file="$3"
    local resource_link="dbs/${DATABASE_NAME}/colls/${container}"
    local doc_count
    doc_count=$(jq '. | length' "$json_file")

    echo "Inserting $doc_count documents from $(basename "$json_file") into '$container'..."

    for i in $(seq 0 $((doc_count - 1))); do
        local doc
        doc=$(jq -c ".[$i]" "$json_file")
        local doc_id
        doc_id=$(echo "$doc" | jq -r '.id')
        local partition_value
        partition_value=$(echo "$doc" | jq -r ".${partition_key_field}")

        generate_auth "POST" "docs" "$resource_link"
        HTTP_CODE=$(curl -k -s -o /dev/null -w "%{http_code}" \
            -X POST "${COSMOS_ENDPOINT}/dbs/${DATABASE_NAME}/colls/${container}/docs" \
            -H "Authorization: ${AUTH_TOKEN}" \
            -H "x-ms-date: ${AUTH_DATE}" \
            -H "x-ms-version: 2018-12-31" \
            -H "x-ms-documentdb-partitionkey: [\"${partition_value}\"]" \
            -H "Content-Type: application/json" \
            -d "$doc")

        if [ "$HTTP_CODE" = "201" ]; then
            insert_count=$((insert_count + 1))
        elif [ "$HTTP_CODE" = "409" ]; then
            skip_count=$((skip_count + 1))
        else
            echo "    WARNING: Unexpected response $HTTP_CODE inserting doc '$doc_id'."
        fi
    done
}

# Insert records container seed data
for seed_file in "$SEED_DATA_DIR"/activity-seed.json \
                 "$SEED_DATA_DIR"/food-seed.json \
                 "$SEED_DATA_DIR"/sleep-seed.json \
                 "$SEED_DATA_DIR"/vitals-seed.json; do
    if [ -f "$seed_file" ]; then
        insert_documents "records" "documentType" "$seed_file"
    fi
done

# Insert conversations seed data
if [ -f "$SEED_DATA_DIR/conversations-seed.json" ]; then
    insert_documents "conversations" "sessionId" "$SEED_DATA_DIR/conversations-seed.json"
fi

echo ""
echo "=== Cosmos DB Initialization Complete ==="
echo "  Documents inserted: $insert_count"
echo "  Documents skipped (already exist): $skip_count"
echo "  Total: $((insert_count + skip_count))"

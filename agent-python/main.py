# -*- coding: utf-8 -*-
import os
import time
import json
import requests
from dotenv import load_dotenv

import firebase_admin
from firebase_admin import credentials, firestore

load_dotenv()

# -----------------------------
# LLM (Ollama) config
# -----------------------------
OLLAMA_BASE = os.getenv("OLLAMA_BASE", "http://localhost:11434")
OLLAMA_MODEL = os.getenv("OLLAMA_MODEL", "llama3.1")

SYSTEM_PROMPT = """
You are an intent and slot extraction engine for a banking billing assistant.
Return ONLY valid JSON. No markdown. No extra text.

Allowed intents: query_bill, query_bill_detailed, pay_bill, help, unknown.

Slots:
- subscriberNo: string
- year: integer
- month: integer (1-12)
- page: integer (optional)
- pageSize: integer (optional)
- amount: number (optional)

Rules:
- If required info is missing, put missing field names in "missing" and set "ask" to a single short question.
- If user asks "detailed", use query_bill_detailed.
- If user asks to pay, use pay_bill.
- If month/year not provided, ask for them.

Output schema:
{
  "intent": "...",
  "slots": { "subscriberNo": "...", "year": 2024, "month": 10, "page": 1, "pageSize": 5, "amount": 300 },
  "missing": [],
  "ask": ""
}
""".strip()


def llm_extract_intent(user_text: str) -> dict:
    payload = {
        "model": OLLAMA_MODEL,
        "messages": [
            {"role": "system", "content": SYSTEM_PROMPT},
            {"role": "user", "content": user_text}
        ],
        "stream": False
    }

    r = requests.post(f"{OLLAMA_BASE}/api/chat", json=payload, timeout=60)
    r.raise_for_status()

    content = (r.json().get("message", {}) or {}).get("content", "").strip()

    start = content.find("{")
    end = content.rfind("}")
    if start == -1 or end == -1:
        return {
            "intent": "unknown",
            "slots": {},
            "missing": [],
            "ask": "I couldn't parse your request. Try again."
        }

    content_json = content[start:end + 1]
    try:
        return json.loads(content_json)
    except Exception:
        return {
            "intent": "unknown",
            "slots": {},
            "missing": [],
            "ask": "I couldn't parse your request. Try again."
        }


# -----------------------------
# Firestore / Gateway config
# -----------------------------
CONV_ID = os.getenv("FIRESTORE_CONVERSATION_ID", "demo-conv-1")
GATEWAY_BASE = os.getenv("GATEWAY_BASE", "http://localhost:8080")
CREDS_PATH = os.getenv("GOOGLE_APPLICATION_CREDENTIALS", "serviceAccountKey.json")

cred = credentials.Certificate(CREDS_PATH)
# ✅ double init guard
if not firebase_admin._apps:
    firebase_admin.initialize_app(cred)

db = firestore.client()

messages_ref = (
    db.collection("conversations")
      .document(CONV_ID)
      .collection("messages")
)


# -----------------------------
# Helpers
# -----------------------------
def post_assistant(text: str, meta: dict | None = None):
    payload = {
        "role": "assistant",
        "text": text,
        "status": "sent",
        "createdAt": firestore.SERVER_TIMESTAMP
    }
    if meta is not None:
        payload["meta"] = meta
    messages_ref.add(payload)


def get_last_by_kind(kind: str):
    docs = (
        messages_ref
        .order_by("createdAt", direction=firestore.Query.DESCENDING)
        .limit(120)
        .stream()
    )

    for d in docs:
        m = d.to_dict() or {}
        if m.get("role") != "assistant":
            continue

        txt = (m.get("text") or "").strip()
        if txt.startswith("⚠️") or txt.startswith("⚠"):
            continue

        meta = m.get("meta") or {}
        if meta.get("kind") == kind:
            return txt, meta

    return None, None


def safe_int(x, default):
    try:
        if x is None:
            return default
        return int(str(x).strip())
    except Exception:
        return default


def safe_str(x, default):
    s = str(x).strip() if x is not None else ""
    return s if s else default


def ui_subscriber(ui_meta: dict):
    # ✅ UI bazen subscriberNo yerine subscriber yollar
    return (
        ui_meta.get("subscriberNo")
        or ui_meta.get("subscriber")
        or ui_meta.get("subscriberId")
        or ui_meta.get("subscriber_id")
    )


def handle_user_message(msg: dict):
    """
    Öncelik sırası:
    1) Kullanıcı metni sadece sayı ise -> subscriber override (LLM yok)
    2) UI meta.intent varsa -> LLM yok -> direkt gateway call
    3) Yoksa LLM ile intent/slot çıkar
    4) 429 olursa cache göster
    """

    ui_meta = msg.get("meta") or {}
    ui_intent = (ui_meta.get("intent") or "").strip()

    user_text = (msg.get("text") or "").strip()

    # ✅ UI butonu text boş gönderirse de çalışsın
    if not user_text and ui_intent not in ["query_bill", "query_bill_detailed"]:
        return

    # ---- 1) Eğer user_text sadece sayı ise -> subscriber override
    only_digits = user_text.replace(" ", "")
    if only_digits.isdigit() and 3 <= len(only_digits) <= 12:
        intent = "query_bill"
        slots = {
            "subscriberNo": only_digits,
            "year": 2024,
            "month": 10
        }
        missing = []
        ask = ""

    # ---- 2) UI meta intent varsa -> LLM bypass
    elif ui_intent in ["query_bill", "query_bill_detailed"]:
        intent = ui_intent
        slots = {
            "subscriberNo": ui_subscriber(ui_meta),  # ✅ fallback burada
            "year": ui_meta.get("year"),
            "month": ui_meta.get("month"),
            "page": ui_meta.get("page"),
            "pageSize": ui_meta.get("pageSize"),
            "amount": ui_meta.get("amount"),
        }
        missing = []
        ask = ""

    # ---- 3) Son çare: LLM parse
    else:
        try:
            extracted = llm_extract_intent(user_text)
        except Exception as e:
            post_assistant(f"LLM error: {e}")
            return

        intent = (extracted.get("intent") or "unknown").strip()
        slots = extracted.get("slots") or {}
        missing = extracted.get("missing") or []
        ask = (extracted.get("ask") or "").strip()

    # Help/Unknown
    if intent in ["help", "unknown"]:
        post_assistant(
            "Commands:\n"
            "- Use the portal controls (Subscriber/Year/Month) and click Query Bill or Query Detailed\n"
            "- Or type: 'Show my bill for subscriber 1001 October 2024'\n"
            "- Pay using the Pay Now button."
        )
        return

    # Pay intent (UI Pay Now)
    if intent == "pay_bill":
        post_assistant("Please use the Pay Now button to make a payment.")
        return

    # Query intents
    if intent not in ["query_bill", "query_bill_detailed"]:
        post_assistant("I didn't understand. Try Help, or use the portal Query buttons.")
        return

    # ✅ required slot check (LLM missing listine güvenme)
    subscriber_no = safe_str(slots.get("subscriberNo"), "")
    year = safe_int(slots.get("year"), None)
    month = safe_int(slots.get("month"), None)

    if not subscriber_no:
        post_assistant("What is your subscriber number?")
        return
    if year is None:
        post_assistant("What year would you like to check the bill for?")
        return
    if month is None:
        post_assistant("Which month would you like to query? (1-12)")
        return

    is_detailed = (intent == "query_bill_detailed")
    endpoint = "/gw/query-bill-detailed" if is_detailed else "/gw/query-bill"

    try:
        r = requests.get(
            f"{GATEWAY_BASE}{endpoint}",
            params={"subscriberNo": subscriber_no, "year": year, "month": month},
            timeout=10
        )

        if r.status_code != 200:
            if r.status_code == 429:
                fallback_kind = "bill_detailed" if is_detailed else "bill_summary"
                last_text, last_meta = get_last_by_kind(fallback_kind)
                if last_text and last_meta:
                    post_assistant(last_text, meta=last_meta)
                else:
                    post_assistant("Daily query limit reached. Please try again later.")
            else:
                post_assistant("The service is temporarily unavailable. Please try again.")
            return

        data = r.json()

        def pick_items(d: dict):
            for k in ["items", "billItems", "breakdown", "details", "lineItems"]:
                v = d.get(k)
                if isinstance(v, list):
                    return v
            return []

        items = pick_items(data)
        is_paid = bool(data.get("isPaid"))
        bill_total = data.get("billTotal") or 0

        if is_detailed:
            lines = []
            for it in items[:20]:
                desc = it.get("description") or it.get("name") or "Item"
                amt = it.get("amount") or 0
                typ = it.get("itemType") or ""
                extra = f" ({typ})" if typ else ""
                lines.append(f"- {desc}: {amt}{extra}")

            reply = (
                f"Bill Detailed for {data.get('month')}/{data.get('year')} (Subscriber {data.get('subscriberNo')}):\n"
                f"- Total: {bill_total}\n"
                f"- Status: {'Paid' if is_paid else 'Unpaid'}\n"
                "Breakdown:\n" + ("\n".join(lines) if lines else "- (no items)")
            )

            meta = {
                "kind": "bill_detailed",
                "subscriberNo": str(data.get("subscriberNo")),
                "year": int(data.get("year")),
                "month": int(data.get("month")),
                "total": float(bill_total),
                "isPaid": is_paid,
                "items": items,
            }
        else:
            reply = (
                f"Bill for {data.get('month')}/{data.get('year')} (Subscriber {data.get('subscriberNo')}):\n"
                f"- Amount Due: {bill_total}\n"
                f"- Status: {'Paid' if is_paid else 'Unpaid'}"
            )

            meta = {
                "kind": "bill_summary",
                "subscriberNo": str(data.get("subscriberNo")),
                "year": int(data.get("year")),
                "month": int(data.get("month")),
                "total": float(bill_total),
                "isPaid": is_paid,
                "canPay": (not is_paid),
                "payAmount": float(bill_total)
            }

        post_assistant(reply, meta=meta)

    except Exception as e:
        post_assistant(f"Agent exception: {e}")


def start_listener():
    print(f"Listening Firestore: conversations/{CONV_ID}/messages")
    print(f"Using gateway: {GATEWAY_BASE}")
    print(f"Using ollama: {OLLAMA_BASE} model={OLLAMA_MODEL}")

    processed_ids = set()

    # ✅ Sadece user + status=sent dinle (spam azaltır)
    q = (
        messages_ref
        .where("role", "==", "user")
        .where("status", "==", "sent")
    )

    def on_snapshot(col_snapshot, changes, read_time):
        for change in changes:
            if change.type.name != "ADDED":
                continue

            doc = change.document
            doc_id = doc.id

            # ✅ dedupe
            if doc_id in processed_ids:
                continue
            processed_ids.add(doc_id)

            msg = doc.to_dict() or {}
            status = msg.get("status")
            if status != "sent":
                continue

            text = (msg.get("text") or "").strip()
            meta = msg.get("meta") or {}
            ui_intent = (meta.get("intent") or "").strip()

            # UI intent varsa text boş olabilir
            if not text and ui_intent not in ["query_bill", "query_bill_detailed"]:
                continue

            print("New user msg:", text or f"[UI:{ui_intent}]")

            # idempotency
            try:
                doc.reference.update({"status": "processed"})
            except Exception as e:
                print("Status update warning:", e)

            handle_user_message(msg)

    q.on_snapshot(on_snapshot)

    while True:
        time.sleep(1)


if __name__ == "__main__":
    start_listener()

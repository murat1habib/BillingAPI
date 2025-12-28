import { doc, updateDoc } from "firebase/firestore";
import { useEffect, useMemo, useState } from "react";
import { db } from "./firebase";
import {
    addDoc,
    collection,
    onSnapshot,
    orderBy,
    query,
    serverTimestamp,
} from "firebase/firestore";

export default function App() {
    const [text, setText] = useState("");
    const [msgs, setMsgs] = useState([]);
    const conversationId = "demo-conv-1";

    // Portal filters
    const [subscriberNo, setSubscriberNo] = useState("1001");
    const [year, setYear] = useState(2024);
    const [month, setMonth] = useState(10);

    // UI-only clear chat (does NOT delete Firestore)
    const [clearedAtMs, setClearedAtMs] = useState(() => {
        const v = localStorage.getItem("chatClearedAtMs");
        return v ? Number(v) : 0;
    });

    function clearChatUI() {
        const now = Date.now();
        setClearedAtMs(now);
        localStorage.setItem("chatClearedAtMs", String(now));
    }

    function showAllChatUI() {
        setClearedAtMs(0);
        localStorage.removeItem("chatClearedAtMs");
    }

    const msgsRef = useMemo(
        () => collection(db, "conversations", conversationId, "messages"),
        [conversationId]
    );

    useEffect(() => {
        const q = query(msgsRef, orderBy("createdAt", "asc"));
        return onSnapshot(q, (snap) => {
            setMsgs(snap.docs.map((d) => ({ id: d.id, ...d.data() })));
        });
    }, [msgsRef]);

    const visibleMsgs = useMemo(() => {
        return msgs.filter((m) => {
            const ms = m.createdAt?.toMillis ? m.createdAt.toMillis() : 0;
            return ms >= clearedAtMs;
        });
    }, [msgs, clearedAtMs]);

    async function sendUserMessage(messageText, meta = null) {
        const payload = {
            role: "user",
            text: messageText,
            status: "sent",
            createdAt: serverTimestamp(),
        };
        if (meta) payload.meta = meta;
        await addDoc(msgsRef, payload);
    }

    async function handlePayNow(msgId, meta) {
        try {
            const res = await fetch("http://localhost:8080/gw/pay-bill", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({
                    subscriberNo: meta.subscriberNo,
                    year: meta.year,
                    month: meta.month,
                    amount: meta.payAmount,
                }),
            });

            const data = await res.json();

            const status = String(data.paymentStatus ?? "").toLowerCase();
            const msg = String(data.message ?? "").toLowerCase();
            const remaining = Number(data.remainingAmount);

            const shouldClosePayButton =
                status === "successful" ||
                remaining === 0 ||
                msg.includes("fully paid") ||
                msg.includes("already fully paid");

            if (shouldClosePayButton) {
                await updateDoc(
                    doc(db, "conversations", conversationId, "messages", msgId),
                    { "meta.canPay": false, "meta.isPaid": true, "meta.payAmount": 0 }
                );
            }

            await addDoc(msgsRef, {
                role: "assistant",
                text: `Payment: ${data.paymentStatus} - ${data.message} (Remaining: ${data.remainingAmount ?? "?"
                    })`,
                status: "sent",
                createdAt: serverTimestamp(),
                meta: { kind: "payment_result", ...data },
            });
        } catch (e) {
            await addDoc(msgsRef, {
                role: "assistant",
                text: `Payment failed: ${e.message}`,
                status: "sent",
                createdAt: serverTimestamp(),
            });
        }
    }

    const btnPrimary = {
        borderRadius: 14,
        padding: "12px 14px",
        background:
            "linear-gradient(180deg, rgba(59,130,246,0.95), rgba(37,99,235,0.95))",
        border: "1px solid rgba(59,130,246,0.55)",
        color: "white",
        fontWeight: 650,
        cursor: "pointer",
        whiteSpace: "nowrap",
    };

    const btnSecondary = {
        borderRadius: 14,
        padding: "12px 14px",
        background: "rgba(255,255,255,0.07)",
        border: "1px solid rgba(255,255,255,0.12)",
        color: "#e5e7eb",
        fontWeight: 600,
        cursor: "pointer",
        whiteSpace: "nowrap",
    };

    const btnGhost = {
        borderRadius: 12,
        padding: "10px 12px",
        background: "transparent",
        border: "1px solid rgba(255,255,255,0.12)",
        color: "rgba(229,231,235,0.9)",
        cursor: "pointer",
        whiteSpace: "nowrap",
    };

    const inputField = {
        borderRadius: 12,
        padding: "10px 10px",
        background: "rgba(255,255,255,0.06)",
        border: "1px solid rgba(255,255,255,0.12)",
        color: "#e5e7eb",
        outline: "none",
    };

    function clampMonth(n) {
        const x = Number(n);
        if (Number.isNaN(x)) return 1;
        return Math.min(12, Math.max(1, x));
    }

    function clampYear(n) {
        const x = Number(n);
        if (Number.isNaN(x)) return 2024;
        return Math.min(2100, Math.max(1990, x));
    }

    // ✅ If user types ONLY digits (e.g. "1001"), also update subscriberNo state
    function maybeUpdateSubscriberFromText(t) {
        const trimmed = (t ?? "").trim();
        if (/^\d{3,12}$/.test(trimmed)) {
            setSubscriberNo(trimmed);
            return true;
        }
        return false;
    }

    // ✅ Pay Now should depend ONLY on message meta (unpaid => show)
    function shouldShowPayNow(m) {
        if (m.role !== "assistant") return false;

        const kind = m.meta?.kind;
        const isBill = kind === "bill_summary" || kind === "bill_detailed";
        if (!isBill) return false;

        const isPaid = m.meta?.isPaid === true;
        const canPay = m.meta?.canPay !== false; // if undefined -> allow
        const amount = Number(m.meta?.payAmount ?? m.meta?.total ?? 0);

        return !isPaid && canPay && amount > 0;
    }

    return (
        <div
            style={{
                minHeight: "100vh",
                width: "100vw",
                display: "flex",
                alignItems: "center",
                justifyContent: "center",
                padding: 24,
                background:
                    "linear-gradient(180deg, #0b1220 0%, #0f172a 60%, #0b1220 100%)",
                color: "#e5e7eb",
                fontFamily:
                    "Inter, system-ui, -apple-system, Segoe UI, Roboto, sans-serif",
            }}
        >
            <div style={{ width: "100%", maxWidth: 980, margin: "0 auto" }}>
                {/* Header */}
                <div
                    style={{
                        marginBottom: 14,
                        display: "flex",
                        alignItems: "center",
                        justifyContent: "space-between",
                        gap: 12,
                    }}
                >
                    <div>
                        <div style={{ fontSize: 13, opacity: 0.75 }}>Secure Billing Portal</div>
                        <div style={{ fontSize: 22, fontWeight: 750, letterSpacing: 0.2 }}>
                            Billing Agent Chat
                        </div>
                    </div>

                    <div style={{ display: "flex", gap: 10 }}>
                        <button
                            onClick={() => sendUserMessage("Help")}
                            style={btnSecondary}
                            title="Usage hints"
                        >
                            Help
                        </button>
                    </div>
                </div>

                {/* Card */}
                <div
                    style={{
                        borderRadius: 18,
                        background: "rgba(255,255,255,0.06)",
                        border: "1px solid rgba(255,255,255,0.10)",
                        boxShadow: "0 20px 60px rgba(0,0,0,0.35)",
                        overflow: "hidden",
                    }}
                >
                    <div style={{ padding: 16 }}>
                        <div style={{ fontSize: 12, opacity: 0.75, marginBottom: 10 }}>
                            Real-time chat • Firestore • Gateway • .NET Billing API
                        </div>

                        {/* Portal Controls */}
                        <div
                            style={{
                                display: "flex",
                                gap: 10,
                                flexWrap: "wrap",
                                alignItems: "center",
                                marginBottom: 12,
                                padding: 12,
                                borderRadius: 14,
                                border: "1px solid rgba(255,255,255,0.10)",
                                background: "rgba(0,0,0,0.18)",
                            }}
                        >
                            <div style={{ display: "flex", gap: 8, alignItems: "center" }}>
                                <span style={{ fontSize: 12, opacity: 0.75 }}>Subscriber</span>
                                <input
                                    value={subscriberNo}
                                    onChange={(e) => setSubscriberNo(e.target.value)}
                                    style={{ ...inputField, width: 130 }}
                                    placeholder="1001"
                                />
                            </div>

                            <div style={{ display: "flex", gap: 8, alignItems: "center" }}>
                                <span style={{ fontSize: 12, opacity: 0.75 }}>Year</span>
                                <input
                                    type="number"
                                    value={year}
                                    onChange={(e) => setYear(clampYear(e.target.value))}
                                    style={{ ...inputField, width: 100 }}
                                />
                            </div>

                            <div style={{ display: "flex", gap: 8, alignItems: "center" }}>
                                <span style={{ fontSize: 12, opacity: 0.75 }}>Month</span>
                                <input
                                    type="number"
                                    value={month}
                                    min={1}
                                    max={12}
                                    onChange={(e) => setMonth(clampMonth(e.target.value))}
                                    style={{ ...inputField, width: 80 }}
                                />
                            </div>

                            <div style={{ display: "flex", gap: 10, marginLeft: "auto" }}>
                                <button
                                    onClick={() => {
                                        console.log("QUERY BILL META =>", {
                                            intent: "query_bill",
                                            subscriberNo,
                                            year,
                                            month,
                                        });
                                        sendUserMessage("Query Bill", {
                                            intent: "query_bill",
                                            subscriberNo,
                                            year,
                                            month,
                                        });
                                    }}
                                    style={btnPrimary}
                                >
                                    Query Bill
                                </button>

                                <button
                                    onClick={() => {
                                        console.log("QUERY DETAILED META =>", {
                                            intent: "query_bill_detailed",
                                            subscriberNo,
                                            year,
                                            month,
                                        });
                                        sendUserMessage("Query Detailed", {
                                            intent: "query_bill_detailed",
                                            subscriberNo,
                                            year,
                                            month,
                                        });
                                    }}
                                    style={btnSecondary}
                                >
                                    Query Detailed
                                </button>
                            </div>
                        </div>

                        {/* Chat area */}
                        <div
                            style={{
                                borderRadius: 14,
                                border: "1px solid rgba(255,255,255,0.10)",
                                background: "rgba(0,0,0,0.25)",
                                height: 460,
                                overflow: "auto",
                                padding: 14,
                            }}
                        >
                            {visibleMsgs.map((m) => (
                                <div
                                    key={m.id}
                                    style={{
                                        display: "flex",
                                        justifyContent: m.role === "user" ? "flex-end" : "flex-start",
                                        marginBottom: 10,
                                    }}
                                >
                                    <div
                                        style={{
                                            maxWidth: "78%",
                                            padding: "10px 12px",
                                            borderRadius: 14,
                                            lineHeight: 1.35,
                                            fontSize: 14,
                                            background:
                                                m.role === "user"
                                                    ? "rgba(59,130,246,0.25)"
                                                    : "rgba(255,255,255,0.08)",
                                            border:
                                                m.role === "user"
                                                    ? "1px solid rgba(59,130,246,0.35)"
                                                    : "1px solid rgba(255,255,255,0.10)",
                                        }}
                                    >
                                        <div style={{ whiteSpace: "pre-wrap" }}>{m.text}</div>

                                        {/* ✅ Pay Now depends ONLY on meta: unpaid => show */}
                                        {shouldShowPayNow(m) && (
                                            <div style={{ marginTop: 10 }}>
                                                <button
                                                    onClick={() => handlePayNow(m.id, m.meta)}
                                                    style={btnPrimary}
                                                >
                                                    Pay Now • {Number(m.meta?.payAmount ?? m.meta?.total ?? 0)}
                                                </button>
                                            </div>
                                        )}
                                    </div>
                                </div>
                            ))}
                        </div>

                        {/* Composer */}
                        <div style={{ display: "flex", gap: 10, marginTop: 12 }}>
                            <input
                                value={text}
                                onChange={(e) => setText(e.target.value)}
                                placeholder="Type a message…"
                                style={{
                                    ...inputField,
                                    flex: 1,
                                    padding: "12px 12px",
                                    borderRadius: 14,
                                }}
                                onKeyDown={(e) => {
                                    if (e.key === "Enter") {
                                        const t = text.trim();
                                        if (!t) return;

                                        // ✅ If user typed only digits, update subscriber input too
                                        maybeUpdateSubscriberFromText(t);

                                        sendUserMessage(t);
                                        setText("");
                                    }
                                }}
                            />
                            <button
                                onClick={() => {
                                    const t = text.trim();
                                    if (!t) return;

                                    // ✅ If user typed only digits, update subscriber input too
                                    maybeUpdateSubscriberFromText(t);

                                    sendUserMessage(t);
                                    setText("");
                                }}
                                style={btnPrimary}
                            >
                                Send
                            </button>
                        </div>

                        {/* Footer actions */}
                        <div
                            style={{
                                display: "flex",
                                justifyContent: "space-between",
                                marginTop: 10,
                                gap: 10,
                            }}
                        >
                            <button onClick={clearChatUI} style={btnGhost}>
                                Clear Chat (UI)
                            </button>

                            {clearedAtMs > 0 ? (
                                <button onClick={showAllChatUI} style={btnGhost}>
                                    Show All
                                </button>
                            ) : (
                                <div style={{ fontSize: 12, opacity: 0.65, alignSelf: "center" }}>
                                    Tip: use Query buttons above or type a subscriber like “1001”
                                </div>
                            )}
                        </div>
                    </div>
                </div>

                <div style={{ marginTop: 10, fontSize: 12, opacity: 0.6 }}>
                    Conversation: <span style={{ opacity: 0.9 }}>{conversationId}</span>
                </div>
            </div>
        </div>
    );
}

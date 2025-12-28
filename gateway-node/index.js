import express from "express";
import cors from "cors";
import axios from "axios";
import dotenv from "dotenv";

dotenv.config();

const app = express();
app.use(cors());
app.use(express.json());

const PORT = process.env.PORT || 8080;
const DOTNET_BASE = process.env.DOTNET_BASE; // http://localhost:5153

let cachedMobileToken = null;
let cachedMobileTokenExp = 0;

async function getMobileToken() {
    const now = Date.now();
    if (cachedMobileToken && now < cachedMobileTokenExp - 30_000) return cachedMobileToken;

    const res = await axios.post(`${DOTNET_BASE}/api/v1/auth/login`, {
        clientType: "mobile",
        username: process.env.MOBILE_USERNAME || "demo",
        password: process.env.MOBILE_PASSWORD || "demo",
    });

    cachedMobileToken = res.data.token;
    cachedMobileTokenExp = new Date(res.data.expiresAt).getTime();
    return cachedMobileToken;
}


// Health
app.get("/health", (req, res) => {
    res.json({ ok: true, dotnet: DOTNET_BASE });
});

async function forward(req, res, config) {
    try {
        const headers = {};

        // Eðer client Authorization gönderiyorsa onu kullan
        if (req.headers["authorization"]) {
            headers.Authorization = req.headers["authorization"];
        }

        // Yoksa ve mobile endpoint'ine gidiyorsak gateway kendi token'ýný eklesin
        const isMobile = config.path.startsWith("/api/v1/mobile/");
        if (!headers.Authorization && isMobile) {
            const token = await getMobileToken();
            headers.Authorization = `Bearer ${token}`;
        }

        const r = await axios({
            method: config.method,
            url: `${DOTNET_BASE}${config.path}`,
            params: config.params,
            data: config.data,
            headers,
            validateStatus: () => true,
        });

        return res.status(r.status).json(r.data);
    } catch (err) {
        console.error("Gateway error:", err?.message);
        return res.status(500).json({ error: "Gateway error", detail: err?.message });
    }
}


/**
 * GW: Query Bill (mobile)
 * GET /gw/query-bill?subscriberNo=1001&year=2024&month=10
 * -> GET /api/v1/mobile/query-bill
 */
app.get("/gw/query-bill", (req, res) =>
    forward(req, res, {
        method: "GET",
        path: "/api/v1/mobile/query-bill",
        params: {
            subscriberNo: req.query.subscriberNo,
            year: req.query.year,
            month: req.query.month,
        },
    })
);


/**
 * GW: Query Bill Detailed (add this endpoint in .NET if not exists yet!)
 * GET /gw/query-bill-detailed?subscriberNo=1001&year=2024&month=10&page=1&pageSize=2
 */
app.get("/gw/query-bill-detailed", (req, res) =>
    forward(req, res, {
        method: "GET",
        path: "/api/v1/mobile/query-bill-detailed",
        params: {
            subscriberNo: req.query.subscriberNo,
            year: req.query.year,
            month: req.query.month,
            page: req.query.page,
            pageSize: req.query.pageSize,
        },
    })
);

/**
 * GW: Pay Bill (website)
 * POST /gw/pay-bill
 * body: { subscriberNo, year, month, amount }
 */
app.post("/gw/pay-bill", (req, res) =>
    forward(req, res, {
        method: "POST",
        path: "/api/v1/website/pay-bill",
        data: req.body,
    })
);

app.listen(PORT, () => {
    console.log(`Gateway running: http://localhost:${PORT}`);
    console.log(`Forwarding to: ${DOTNET_BASE}`);
});

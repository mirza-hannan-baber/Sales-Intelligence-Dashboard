import { useState, useEffect } from "react";
import {
  Search,
  Filter,
  Plus,
  DollarSign,
  Target,
  BriefcaseBusiness,
  Loader2,
} from "lucide-react";
import { dealsService } from "../services/api";

export default function Deals() {
  const [deals, setDeals] = useState([]);
  const [stats, setStats] = useState({
    totalPipeline: "$0",
    totalDeals: 0,
    avgProbability: "0%",
  });
  const [filterOptions, setFilterOptions] = useState({
    products: [],
    owners: [],
    sectors: [],
  });
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [statusFilter, setStatusFilter] = useState("All");
  const [productFilter, setProductFilter] = useState("All");
  const [ownerFilter, setOwnerFilter] = useState("All");
  const [sectorFilter, setSectorFilter] = useState("All");
  const [page, setPage] = useState(1);
  const pageSize = 50;

  const [showModal, setShowModal] = useState(false);
  const [newDeal, setNewDeal] = useState({
    name: "",
    company: "",
    owner: "",
    product: "GTXPro",
    sector: "Technology",
    value: 50000,
    status: "In Progress",
  });

  useEffect(() => {
    const t = setTimeout(() => setDebouncedSearch(search), 300);
    return () => clearTimeout(t);
  }, [search]);

  useEffect(() => {
    setPage(1);
  }, [debouncedSearch, statusFilter, productFilter, ownerFilter, sectorFilter]);

  useEffect(() => {
    fetchDeals();
  }, [debouncedSearch, statusFilter, productFilter, ownerFilter, sectorFilter, page]);

  const fetchDeals = async () => {
    setLoading(true);
    try {
      const data = await dealsService.getDeals({
        search: debouncedSearch,
        status: statusFilter,
        product: productFilter,
        owner: ownerFilter,
        sector: sectorFilter,
        page,
        pageSize,
      });
      setDeals(data.items || []);
      setStats({
        totalPipeline: data.totalPipeline,
        totalDeals: data.totalDeals,
        avgProbability: data.avgProbability,
      });
      if (data.filterOptions) {
        setFilterOptions({
          products: data.filterOptions.products || [],
          owners: data.filterOptions.owners || [],
          sectors: data.filterOptions.sectors || [],
        });
      }
    } catch (err) {
      console.error("Error fetching deals:", err);
    } finally {
      setLoading(false);
    }
  };

  const handleCreateDeal = async (e) => {
    e.preventDefault();
    try {
      await dealsService.createDeal({
        ...newDeal,
        value: Number(newDeal.value),
        probability: newDeal.status === "Won" ? 100 : newDeal.status === "Lost" ? 0 : 65,
      });
      setShowModal(false);
      setNewDeal({
        name: "",
        company: "",
        owner: "",
        product: "GTXPro",
        sector: "Technology",
        value: 50000,
        status: "In Progress",
      });
      fetchDeals();
    } catch (err) {
      console.error("Error creating deal:", err);
    }
  };

  const totalPages = Math.max(1, Math.ceil(stats.totalDeals / pageSize));

  const statCards = [
    {
      title: "Total Pipeline",
      value: stats.totalPipeline,
      change: `${stats.totalDeals} Deals`,
      icon: DollarSign,
    },
    {
      title: "Total Deals",
      value: stats.totalDeals.toString(),
      change: "Matching filters",
      icon: BriefcaseBusiness,
    },
    {
      title: "Avg Win Probability",
      value: stats.avgProbability,
      change: "From database",
      icon: Target,
    },
  ];

  return (
    <div className="deals-page">
      <div className="page-title">
        <div>
          <h1>Deals</h1>
          <p>Manage and monitor sales opportunities from the live CRM database</p>
        </div>
        <button className="primary-button" onClick={() => setShowModal(true)}>
          <Plus size={17} />
          Create Deal
        </button>
      </div>

      <div className="stats-grid">
        {statCards.map((stat) => {
          const Icon = stat.icon;
          return (
            <div className="stat-card" key={stat.title}>
              <div className="stat-card-top">
                <div className="stat-icon">
                  <Icon size={19} />
                </div>
                <span className="stat-change">{stat.change}</span>
              </div>
              <p>{stat.title}</p>
              <h2>{stat.value}</h2>
            </div>
          );
        })}
      </div>

      <div className="deals-table-card">
        <div className="deals-toolbar" style={{ flexWrap: "wrap", gap: "10px" }}>
          <div className="deals-search">
            <Search size={17} />
            <input
              type="text"
              placeholder="Search deals, companies or owners..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
            />
          </div>

          <div className="deals-filter">
            <Filter size={16} />
            <select value={statusFilter} onChange={(e) => setStatusFilter(e.target.value)}>
              <option value="All">All Status</option>
              <option value="Won">Won</option>
              <option value="In Progress">In Progress</option>
              <option value="Lost">Lost</option>
            </select>
          </div>

          <select
            className="deals-filter"
            value={productFilter}
            onChange={(e) => setProductFilter(e.target.value)}
          >
            <option value="All">All Products</option>
            {filterOptions.products.map((p) => (
              <option key={p} value={p}>{p}</option>
            ))}
          </select>

          <select
            className="deals-filter"
            value={ownerFilter}
            onChange={(e) => setOwnerFilter(e.target.value)}
          >
            <option value="All">All Owners</option>
            {filterOptions.owners.map((o) => (
              <option key={o} value={o}>{o}</option>
            ))}
          </select>

          <select
            className="deals-filter"
            value={sectorFilter}
            onChange={(e) => setSectorFilter(e.target.value)}
          >
            <option value="All">All Sectors</option>
            {filterOptions.sectors.map((s) => (
              <option key={s} value={s}>{s}</option>
            ))}
          </select>
        </div>

        <div className="deals-table-wrapper">
          {loading ? (
            <div style={{ display: "flex", justifyContent: "center", padding: "40px", color: "#6366f1" }}>
              <Loader2 className="animate-spin" size={24} />
            </div>
          ) : (
            <table className="deals-table">
              <thead>
                <tr>
                  <th>Deal</th>
                  <th>Company</th>
                  <th>Owner</th>
                  <th>Product</th>
                  <th>Deal Value</th>
                  <th>Win Probability</th>
                  <th>Status</th>
                </tr>
              </thead>
              <tbody>
                {deals.map((deal) => (
                  <tr key={deal.id}>
                    <td>
                      <strong>{deal.name}</strong>
                    </td>
                    <td>{deal.company}</td>
                    <td>{deal.owner}</td>
                    <td>{deal.product}</td>
                    <td>${Number(deal.value).toLocaleString()}</td>
                    <td>
                      <div className="deal-probability">
                        <div className="deal-probability-bar">
                          <div
                            className="deal-probability-fill"
                            style={{ width: `${deal.probability}%` }}
                          />
                        </div>
                        <span>{deal.probability}%</span>
                      </div>
                    </td>
                    <td>
                      <span
                        className={`deal-status-badge ${
                          deal.status === "Won"
                            ? "deal-won"
                            : deal.status === "Lost"
                            ? "deal-risk"
                            : "deal-progress"
                        }`}
                      >
                        {deal.status}
                      </span>
                    </td>
                  </tr>
                ))}
                {deals.length === 0 && (
                  <tr>
                    <td colSpan="7" className="no-deals">
                      No deals found.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          )}
        </div>

        <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", padding: "12px 16px", borderTop: "1px solid #e2e8f0" }}>
          <span style={{ fontSize: "0.85rem", color: "#64748b" }}>
            Page {page} of {totalPages} · {stats.totalDeals} deals
          </span>
          <div style={{ display: "flex", gap: "8px" }}>
            <button
              className="secondary-button"
              disabled={page <= 1 || loading}
              onClick={() => setPage((p) => Math.max(1, p - 1))}
              style={{ padding: "6px 12px" }}
            >
              Previous
            </button>
            <button
              className="secondary-button"
              disabled={page >= totalPages || loading}
              onClick={() => setPage((p) => p + 1)}
              style={{ padding: "6px 12px" }}
            >
              Next
            </button>
          </div>
        </div>
      </div>

      {showModal && (
        <div style={{ position: "fixed", inset: 0, background: "rgba(15, 23, 42, 0.75)", display: "flex", justifyContent: "center", alignItems: "center", zIndex: 1000 }}>
          <div style={{ background: "#ffffff", padding: "24px", borderRadius: "12px", width: "100%", maxWidth: "450px" }}>
            <h2 style={{ marginBottom: "16px" }}>Create New Deal</h2>
            <form onSubmit={handleCreateDeal}>
              <div style={{ marginBottom: "12px" }}>
                <label style={{ display: "block", fontSize: "0.85rem", fontWeight: "600", marginBottom: "4px" }}>Deal Name</label>
                <input type="text" required value={newDeal.name} onChange={(e) => setNewDeal({ ...newDeal, name: e.target.value })} style={{ width: "100%", padding: "8px", borderRadius: "6px", border: "1px solid #cbd5e1" }} />
              </div>
              <div style={{ marginBottom: "12px" }}>
                <label style={{ display: "block", fontSize: "0.85rem", fontWeight: "600", marginBottom: "4px" }}>Company / Account</label>
                <input type="text" required value={newDeal.company} onChange={(e) => setNewDeal({ ...newDeal, company: e.target.value })} style={{ width: "100%", padding: "8px", borderRadius: "6px", border: "1px solid #cbd5e1" }} />
              </div>
              <div style={{ marginBottom: "12px" }}>
                <label style={{ display: "block", fontSize: "0.85rem", fontWeight: "600", marginBottom: "4px" }}>Sales Owner</label>
                <input type="text" required value={newDeal.owner} onChange={(e) => setNewDeal({ ...newDeal, owner: e.target.value })} style={{ width: "100%", padding: "8px", borderRadius: "6px", border: "1px solid #cbd5e1" }} />
              </div>
              <div style={{ marginBottom: "12px" }}>
                <label style={{ display: "block", fontSize: "0.85rem", fontWeight: "600", marginBottom: "4px" }}>Deal Value ($)</label>
                <input type="number" required value={newDeal.value} onChange={(e) => setNewDeal({ ...newDeal, value: e.target.value })} style={{ width: "100%", padding: "8px", borderRadius: "6px", border: "1px solid #cbd5e1" }} />
              </div>
              <div style={{ display: "flex", justifyContent: "flex-end", gap: "8px", marginTop: "20px" }}>
                <button type="button" onClick={() => setShowModal(false)} className="secondary-button" style={{ padding: "8px 16px" }}>Cancel</button>
                <button type="submit" className="primary-button" style={{ padding: "8px 16px" }}>Save Deal</button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}

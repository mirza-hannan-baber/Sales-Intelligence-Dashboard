import { useState, useRef } from "react";
import { Upload, X, FileSpreadsheet, CheckCircle2, AlertCircle, Loader2, Trash2 } from "lucide-react";
import { useDataset } from "../context/DatasetContext";

export default function UploadModal() {
  const { isUploadModalOpen, setIsUploadModalOpen, uploadDataset, datasets, deleteDataset } = useDataset();
  const [selectedFile, setSelectedFile] = useState(null);
  const [uploading, setUploading] = useState(false);
  const [error, setError] = useState(null);
  const [successMsg, setSuccessMsg] = useState(null);
  const fileInputRef = useRef(null);

  if (!isUploadModalOpen) return null;

  const handleFileChange = (e) => {
    if (e.target.files && e.target.files[0]) {
      setSelectedFile(e.target.files[0]);
      setError(null);
      setSuccessMsg(null);
    }
  };

  const handleDrop = (e) => {
    e.preventDefault();
    if (e.dataTransfer.files && e.dataTransfer.files[0]) {
      setSelectedFile(e.dataTransfer.files[0]);
      setError(null);
      setSuccessMsg(null);
    }
  };

  const handleDragOver = (e) => {
    e.preventDefault();
  };

  const handleUploadSubmit = async () => {
    if (!selectedFile) {
      setError("Please select a CSV or Excel file to upload.");
      return;
    }

    setUploading(true);
    setError(null);
    setSuccessMsg(null);

    try {
      const result = await uploadDataset(selectedFile);
      setSuccessMsg(`Successfully uploaded dataset "${result.name}" with ${result.dealsCount || result.rowCount} rows!`);
      setTimeout(() => {
        setIsUploadModalOpen(false);
        setSelectedFile(null);
        setSuccessMsg(null);
      }, 1500);
    } catch (err) {
      console.error("Upload error:", err);
      setError(
        err.response?.data?.message ||
          "Failed to upload dataset. Ensure columns match the dataset schema."
      );
    } finally {
      setUploading(false);
    }
  };

  const handleClose = () => {
    if (uploading) return;
    setIsUploadModalOpen(false);
    setSelectedFile(null);
    setError(null);
    setSuccessMsg(null);
  };

  return (
    <div
      style={{
        position: "fixed",
        top: 0,
        left: 0,
        right: 0,
        bottom: 0,
        backgroundColor: "rgba(15, 23, 42, 0.75)",
        backdropFilter: "blur(6px)",
        display: "flex",
        justifyContent: "center",
        alignItems: "center",
        zIndex: 2000,
        padding: "20px",
      }}
      onClick={handleClose}
    >
      <div
        style={{
          background: "#1e293b",
          border: "1px solid #334155",
          borderRadius: "16px",
          width: "100%",
          maxWidth: "520px",
          boxShadow: "0 25px 50px -12px rgba(0, 0, 0, 0.5)",
          color: "#f8fafc",
          overflow: "hidden",
        }}
        onClick={(e) => e.stopPropagation()}
      >
        {/* MODAL HEADER */}
        <div
          style={{
            display: "flex",
            justifyContent: "space-between",
            alignItems: "center",
            padding: "20px 24px",
            borderBottom: "1px solid #334155",
          }}
        >
          <div style={{ display: "flex", alignItems: "center", gap: "10px" }}>
            <div
              style={{
                background: "rgba(99, 102, 241, 0.15)",
                color: "#818cf8",
                padding: "8px",
                borderRadius: "10px",
              }}
            >
              <Upload size={22} />
            </div>
            <div>
              <h3 style={{ margin: 0, fontSize: "1.1rem", color: "#f8fafc" }}>Upload New Dataset</h3>
              <p style={{ margin: 0, fontSize: "0.8rem", color: "#94a3b8" }}>
                Upload CSV or Excel file to automatically update CRM database & charts
              </p>
            </div>
          </div>
          <button
            onClick={handleClose}
            style={{
              background: "transparent",
              border: "none",
              color: "#94a3b8",
              cursor: "pointer",
              padding: "4px",
              display: "flex",
              alignItems: "center",
            }}
          >
            <X size={20} />
          </button>
        </div>

        {/* MODAL BODY */}
        <div style={{ padding: "24px" }}>
          {error && (
            <div
              style={{
                background: "rgba(239, 68, 68, 0.15)",
                border: "1px solid #ef4444",
                color: "#fca5a5",
                padding: "12px 16px",
                borderRadius: "8px",
                fontSize: "0.875rem",
                marginBottom: "16px",
                display: "flex",
                alignItems: "center",
                gap: "10px",
              }}
            >
              <AlertCircle size={18} color="#f87171" />
              <span>{error}</span>
            </div>
          )}

          {successMsg && (
            <div
              style={{
                background: "rgba(34, 197, 94, 0.15)",
                border: "1px solid #22c55e",
                color: "#86efac",
                padding: "12px 16px",
                borderRadius: "8px",
                fontSize: "0.875rem",
                marginBottom: "16px",
                display: "flex",
                alignItems: "center",
                gap: "10px",
              }}
            >
              <CheckCircle2 size={18} color="#4ade80" />
              <span>{successMsg}</span>
            </div>
          )}

          {/* DROP ZONE */}
          <div
            onDrop={handleDrop}
            onDragOver={handleDragOver}
            onClick={() => fileInputRef.current?.click()}
            style={{
              border: "2px dashed #475569",
              borderRadius: "12px",
              padding: "32px 20px",
              textAlign: "center",
              cursor: "pointer",
              background: selectedFile ? "rgba(99, 102, 241, 0.08)" : "#0f172a",
              transition: "border-color 0.2s, background 0.2s",
            }}
            onMouseEnter={(e) => (e.currentTarget.style.borderColor = "#6366f1")}
            onMouseLeave={(e) => (e.currentTarget.style.borderColor = "#475569")}
          >
            <input
              type="file"
              ref={fileInputRef}
              onChange={handleFileChange}
              accept=".csv,.xlsx,.xls,.txt"
              style={{ display: "none" }}
            />

            <FileSpreadsheet
              size={40}
              color={selectedFile ? "#818cf8" : "#64748b"}
              style={{ marginBottom: "12px" }}
            />

            {selectedFile ? (
              <div>
                <strong style={{ display: "block", color: "#f8fafc", fontSize: "0.95rem" }}>
                  {selectedFile.name}
                </strong>
                <span style={{ fontSize: "0.8rem", color: "#94a3b8" }}>
                  {(selectedFile.size / 1024).toFixed(1)} KB — Ready to process
                </span>
              </div>
            ) : (
              <div>
                <p style={{ margin: "0 0 6px 0", fontWeight: 600, color: "#f8fafc" }}>
                  Click to select or drag & drop file here
                </p>
                <span style={{ fontSize: "0.8rem", color: "#64748b" }}>
                  Supports .csv, .xlsx, .xls dataset extracts
                </span>
              </div>
            )}
          </div>

          {/* DATASETS MANAGEMENT SECTION */}
          {datasets && datasets.length > 0 && (
            <div style={{ marginTop: "20px", borderTop: "1px solid #334155", paddingTop: "16px" }}>
              <h4 style={{ margin: "0 0 10px 0", fontSize: "0.85rem", color: "#cbd5e1", textTransform: "uppercase", letterSpacing: "0.5px" }}>
                Uploaded Datasets ({datasets.length})
              </h4>
              <div style={{ maxHeight: "140px", overflowY: "auto", display: "flex", flexDirection: "column", gap: "8px" }}>
                {datasets.map((d) => (
                  <div
                    key={d.id}
                    style={{
                      display: "flex",
                      alignItems: "center",
                      justifyContent: "space-between",
                      background: "#0f172a",
                      border: "1px solid #334155",
                      borderRadius: "8px",
                      padding: "8px 12px",
                      fontSize: "0.85rem",
                    }}
                  >
                    <div>
                      <strong style={{ color: "#f8fafc", display: "block" }}>{d.name}</strong>
                      <span style={{ fontSize: "0.75rem", color: "#94a3b8" }}>
                        {d.dealsCount || d.rowCount} deals · {new Date(d.uploadedAt).toLocaleDateString()}
                      </span>
                    </div>
                    <button
                      type="button"
                      onClick={async () => {
                        const confirmed = window.confirm(`Delete dataset "${d.name}" and all its records from database?`);
                        if (confirmed) {
                          try {
                            await deleteDataset(d.id);
                          } catch (err) {
                            alert(err.response?.data?.message || "Failed to delete dataset.");
                          }
                        }
                      }}
                      style={{
                        background: "rgba(239, 68, 68, 0.15)",
                        border: "1px solid rgba(239, 68, 68, 0.3)",
                        color: "#f87171",
                        borderRadius: "6px",
                        padding: "5px 10px",
                        fontSize: "0.75rem",
                        cursor: "pointer",
                        display: "flex",
                        alignItems: "center",
                        gap: "4px",
                      }}
                    >
                      <Trash2 size={13} />
                      Remove
                    </button>
                  </div>
                ))}
              </div>
            </div>
          )}
        </div>

        {/* MODAL FOOTER */}
        <div
          style={{
            padding: "16px 24px",
            background: "#0f172a",
            borderTop: "1px solid #334155",
            display: "flex",
            justifyContent: "flex-end",
            gap: "12px",
          }}
        >
          <button
            type="button"
            onClick={handleClose}
            disabled={uploading}
            style={{
              padding: "10px 18px",
              borderRadius: "8px",
              border: "1px solid #475569",
              background: "transparent",
              color: "#cbd5e1",
              fontSize: "0.875rem",
              cursor: "pointer",
            }}
          >
            Cancel
          </button>
          <button
            type="button"
            onClick={handleUploadSubmit}
            disabled={uploading || !selectedFile}
            className="primary-button"
            style={{
              display: "flex",
              alignItems: "center",
              gap: "8px",
              padding: "10px 20px",
              opacity: uploading || !selectedFile ? 0.6 : 1,
            }}
          >
            {uploading ? (
              <>
                <Loader2 className="animate-spin" size={16} />
                Processing & Inserting Data...
              </>
            ) : (
              <>
                <Upload size={16} />
                Upload & Insert Data
              </>
            )}
          </button>
        </div>
      </div>
    </div>
  );
}

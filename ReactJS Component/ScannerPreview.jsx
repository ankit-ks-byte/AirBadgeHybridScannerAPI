import React, { useState } from 'react';

const ScannerPreview = () => {
  const [image, setImage] = useState(null);
  const [loading, setLoading] = useState(false);

  const startScan = async () => {
    setLoading(true);
    try {
      const response = await fetch("http://localhost:5001/scan"); // ✅ corrected
      const result = await response.json();
      console.log("📦 API Response:", result);

      if (!result.base64) {
        alert("Scan completed but no image returned.");
        setImage(null);
        return;
      }

      setImage(result.base64);  // ✅ updated
      alert("✅ Scan complete.");
    } catch (error) {
      alert("❌ Failed to start scan or fetch image.");
      console.error(error);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div style={{ padding: 20 }}>
      <h2>📄 Scan Preview</h2>
      <button onClick={startScan} disabled={loading}>
        {loading ? "Scanning..." : "📥 Start Scan"}
      </button>

      <div style={{ marginTop: 20 }}>
        {image && (
          <div style={{ border: "1px solid #ccc", padding: 10 }}>
            <img
              src={image}
              alt="Scanned Document"
              style={{ width: 300, height: "auto" }}
            />
          </div>
        )}
      </div>
    </div>
  );
};

export default ScannerPreview;

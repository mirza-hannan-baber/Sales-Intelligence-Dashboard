import { createContext, useContext, useState, useEffect, useCallback } from 'react';
import { datasetsService } from '../services/api';
import { useAuth } from './AuthContext';

const DatasetContext = createContext(null);

export function DatasetProvider({ children }) {
  const { user } = useAuth();
  const [datasets, setDatasets] = useState([]);
  const [selectedDatasetId, setSelectedDatasetId] = useState(0); // 0 means All Datasets
  const [loadingDatasets, setLoadingDatasets] = useState(false);
  const [isUploadModalOpen, setIsUploadModalOpen] = useState(false);
  const [error, setError] = useState(null);

  const fetchDatasets = useCallback(async () => {
    setLoadingDatasets(true);
    setError(null);
    try {
      const data = await datasetsService.getDatasets({
        userEmail: user?.email,
        role: user?.role,
      });
      const list = data || [];
      setDatasets(list);

      // If selectedDatasetId is not set or not in list (and not 0), default to 0 (All) or first dataset
      if (selectedDatasetId !== 0 && !list.some((d) => d.id === Number(selectedDatasetId))) {
        setSelectedDatasetId(list.length > 0 ? list[0].id : 0);
      }
    } catch (err) {
      console.error('Error fetching datasets:', err);
      setError('Failed to load dataset list.');
    } finally {
      setLoadingDatasets(false);
    }
  }, [selectedDatasetId, user?.email, user?.role]);

  useEffect(() => {
    fetchDatasets();
  }, [user?.email, user?.role]);

  const uploadDataset = async (file) => {
    if (!file) return null;
    const formData = new FormData();
    formData.append('file', file);
    if (user?.email) {
      formData.append('userEmail', user.email);
    }

    const newDataset = await datasetsService.uploadDataset(formData);
    await fetchDatasets();
    if (newDataset && newDataset.id) {
      setSelectedDatasetId(newDataset.id);
    }
    return newDataset;
  };

  const deleteDataset = async (id) => {
    await datasetsService.deleteDataset(id, {
      userEmail: user?.email,
      role: user?.role,
    });
    if (Number(selectedDatasetId) === Number(id)) {
      setSelectedDatasetId(0);
    }
    await fetchDatasets();
  };

  return (
    <DatasetContext.Provider
      value={{
        datasets,
        selectedDatasetId,
        setSelectedDatasetId,
        fetchDatasets,
        uploadDataset,
        deleteDataset,
        loadingDatasets,
        isUploadModalOpen,
        setIsUploadModalOpen,
        error,
      }}
    >
      {children}
    </DatasetContext.Provider>
  );
}

export function useDataset() {
  const context = useContext(DatasetContext);
  if (!context) {
    throw new Error('useDataset must be used within a DatasetProvider');
  }
  return context;
}

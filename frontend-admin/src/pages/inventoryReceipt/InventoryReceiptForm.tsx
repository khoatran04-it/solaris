import React, { useState, useEffect, useCallback } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { Plus, Trash2, X, Save, PackageCheck } from 'lucide-react';

// Common UI
import {
  PageContainer,
  FormCard,
  FormInput,
  FormSelect,
  FormTextarea,
  SubmitButton,
  FormHeader,
  FormSection,
} from '../../components/commons/FormUI';
import { Toast } from '../../components/commons/Toast';

// API & Types
import { inventoryReceiptApi } from '../../api/inventoryReceiptApi';
import { warehouseApi } from '../../api/warehouseApi';
import { supplierApi } from '../../api/supplierApi';
import { productVariantApi } from '../../api/productVariantApi';
import { uomApi } from '../../api/uomApi';
import { productBatchApi } from '../../api/productBatchApi';
import { purchaseOrderApi } from '../../api/purchaseOrderApi';
import { useAuthStore } from '../../stores/useAuthStore';

import { InventoryReceiptCreatePayload } from '../../types/inventoryReceipt';

// Types cho Dropdown
interface SelectOption { value: number; label: string; }

interface DetailRow {
  id: string; // Sử dụng UUID để tránh trùng lặp key
  variantId: number | '';
  batchId: number | '';
  uoMId: number | '';
  purchaseOrderDetailId?: number | null;
  expectedQuantity: number;
  acceptedQuantity: number;
  rejectedQuantity: number;
  rejectReason: string;
}

const InventoryReceiptForm: React.FC = () => {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const poIdParam = searchParams.get('poId');
  const { userInfo } = useAuthStore();

  const [loading, setLoading] = useState(false);
  const [toast, setToast] = useState<{ show: boolean, type: 'success' | 'warning' | 'error', message: string }>({ show: false, type: 'success', message: '' });

  // --- DROPDOWN OPTIONS ---
  const [warehouses, setWarehouses] = useState<SelectOption[]>([]);
  const [suppliers, setSuppliers] = useState<SelectOption[]>([]);
  const [variants, setVariants] = useState<SelectOption[]>([]);
  const [uoms, setUoms] = useState<SelectOption[]>([]);
  const [batches, setBatches] = useState<{ id: number; variantId: number; batchCode: string }[]>([]);

  // --- FORM STATES ---
  const [warehouseId, setWarehouseId] = useState<number | ''>('');
  const [supplierId, setSupplierId] = useState<number | ''>('');
  const [receiptDate, setReceiptDate] = useState(new Date().toLocaleDateString('en-CA'));
  const [notes, setNotes] = useState('');
  
  const [details, setDetails] = useState<DetailRow[]>([
    { id: crypto.randomUUID(), variantId: '', batchId: '', uoMId: '', expectedQuantity: 0, acceptedQuantity: 0, rejectedQuantity: 0, rejectReason: '' },
  ]);
  const [errors, setErrors] = useState<Record<string, string>>({});

  // --- BATCH MODAL STATES ---
  const [showBatchModal, setShowBatchModal] = useState(false);
  const [batchModalRowId, setBatchModalRowId] = useState('');
  const [newBatch, setNewBatch] = useState({
    batchCode: '',
    manufactureDate: new Date().toLocaleDateString('en-CA'),
    expiryDate: '',
  });

  const showToast = (type: 'success' | 'warning' | 'error', message: string) => {
    setToast({ show: true, type, message });
    setTimeout(() => setToast(prev => ({ ...prev, show: false })), 3000);
  };

  // --- EFFECTS ---
  const loadOptions = useCallback(async () => {
    try {
      // 🔥 FIX BUG CHẾT CHÙM API: Bắt lỗi độc lập từng cái bằng .catch(() => [])
      const [whRes, supRes, varRes, uomRes] = await Promise.all([
        warehouseApi.getAllList().catch(() => []),
        supplierApi.getAllList().catch(() => []),
        productVariantApi.getAllList().catch(() => []), 
        uomApi.getAllList().catch(() => []),
      ]);

      // Riêng API Lô hàng (Batch) gọi riêng để tránh sập
      let batchRes: any[] = [];
      try {
        const bData = await productBatchApi.getAllList(); 
        batchRes = bData || [];
      } catch (e) {
        console.warn('API ProductBatch chưa sẵn sàng hoặc rỗng.');
      }

      setWarehouses(whRes.map((w: any) => ({ value: w.id, label: w.name })));
      setSuppliers(supRes.map((s: any) => ({ value: s.id, label: s.name })));
      setVariants(varRes.map((v: any) => ({ value: v.id, label: `${v.code} - ${v.name}` })));
      setUoms(uomRes.map((u: any) => ({ value: u.id, label: u.name })));
      setBatches(batchRes.map(b => ({ id: b.id, variantId: b.variantId, batchCode: b.batchCode })));
    } catch (error) {
      showToast('error', 'Lỗi hệ thống khi tải danh mục bổ trợ!');
    }
  }, []);

  const loadPurchaseOrder = useCallback(async (id: number) => {
    try {
      const data = await purchaseOrderApi.getById(id);
      if (data) {
        setSupplierId(data.supplierId || '');
        if (data.details && data.details.length > 0) {
          const loadedDetails = data.details.map((d: any): DetailRow => {
            const remainingQty = Math.max(0, d.orderQuantity - (d.receivedQuantity || 0));
            return {
              id: crypto.randomUUID(),
              variantId: d.variantId || '',
              batchId: '',
              uoMId: d.uoMId || '',
              purchaseOrderDetailId: d.id,
              expectedQuantity: remainingQty,
              acceptedQuantity: remainingQty,
              rejectedQuantity: 0,
              rejectReason: '',
            };
          });
          setDetails(loadedDetails);
        }
      }
    } catch (error) {
      showToast('error', 'Không thể tải thông tin Đơn mua hàng gốc');
    }
  }, []);

  useEffect(() => {
    loadOptions();
    if (poIdParam) {
      loadPurchaseOrder(parseInt(poIdParam));
    }
  }, [poIdParam, loadOptions, loadPurchaseOrder]);

  // --- HANDLERS ---
  const handleAddRow = () => {
    setDetails([...details, { 
      id: crypto.randomUUID(), 
      variantId: '', batchId: '', uoMId: '', 
      expectedQuantity: 0, acceptedQuantity: 0, rejectedQuantity: 0, 
      rejectReason: '' 
    }]);
  };

  const handleRemoveRow = (id: string) => {
    if (details.length > 1) {
      setDetails(details.filter((d) => d.id !== id));
    }
  };

  const handleDetailChange = (id: string, field: keyof DetailRow, value: any) => {
    setDetails(prev => prev.map(d => {
      if (d.id === id) {
        const updated = { ...d, [field]: value };
        
        if (field === 'rejectedQuantity' && Number(value) === 0) {
          updated.rejectReason = '';
        }
        return updated;
      }
      return d;
    }));
    
    if (errors[`${field}_${id}`]) {
      setErrors(prev => { const newErr = { ...prev }; delete newErr[`${field}_${id}`]; return newErr; });
    }
  };

  // --- BATCH (LÔ HÀNG) HANDLERS ---
  const openBatchModal = (rowId: string, variantId: number | '') => {
    if (!variantId) {
      showToast('warning', 'Vui lòng chọn Sản phẩm trước khi sinh Lô mới!');
      return;
    }
    setBatchModalRowId(rowId);
    
    const todayStr = new Date().toLocaleDateString('en-CA').replace(/-/g, '');
    const randomNum = Math.floor(Math.random() * 1000).toString().padStart(3, '0');
    
    setNewBatch({
      batchCode: `L${todayStr}-${randomNum}`,
      manufactureDate: new Date().toLocaleDateString('en-CA'),
      expiryDate: '',
    });
    setShowBatchModal(true);
  };

  const handleSaveBatch = async () => {
    if (!supplierId) {
        showToast('warning', 'KỶ LUẬT THÉP: Bắt buộc chọn Nhà Cung Cấp ở thông tin chung trước khi tạo Lô!');
        return;
    }

    if (!newBatch.batchCode || !newBatch.manufactureDate || !newBatch.expiryDate) {
        showToast('warning', 'Vui lòng nhập đầy đủ Mã Lô, Ngày SX và Hạn sử dụng!');
        return;
    }

    const targetRow = details.find(d => d.id === batchModalRowId);
    if (!targetRow || !targetRow.variantId) return;

    try {
      // Lót múi giờ trưa UTC để an toàn khi lưu vào DB (tránh bị lùi 1 ngày)
      const safeMfgDate = new Date(`${newBatch.manufactureDate}T12:00:00Z`).toISOString();
      const safeExpDate = new Date(`${newBatch.expiryDate}T12:00:00Z`).toISOString();

      const res = await productBatchApi.create({
        batchCode: newBatch.batchCode,
        variantId: targetRow.variantId as number,
        supplierId: Number(supplierId),
        manufactureDate: safeMfgDate,
        expiryDate: safeExpDate,
        isActive: true
      });

      if (res) {
        // 🔥 Ép kiểu về number để giải quyết triệt để lỗi TypeScript "Type number | undefined"
        const newBatchId = (res.id || res.Id) as number;

        const newBatchObj = { 
            id: newBatchId, 
            variantId: targetRow.variantId as number, 
            batchCode: newBatch.batchCode 
        };

        setBatches(prev => [...prev, newBatchObj]);
        handleDetailChange(batchModalRowId, 'batchId', newBatchId);
        
        setShowBatchModal(false);
        showToast('success', 'Tạo Lô thành công!');
      }
    } catch (error) {
      showToast('error', 'Lỗi server: Không thể tạo Lô hàng!');
    }
  };

  // --- VALIDATION & SUBMIT ---
  const validate = () => {
    const newErrors: Record<string, string> = {};
    if (!warehouseId) newErrors.warehouseId = 'Vui lòng chọn Kho';
    if (!receiptDate) newErrors.receiptDate = 'Vui lòng chọn Ngày';
    
    let hasItems = false;
    details.forEach((d) => {
      if (!d.variantId) newErrors[`variantId_${d.id}`] = 'Trống';
      if (!d.batchId) newErrors[`batchId_${d.id}`] = 'Trống';
      if (!d.uoMId) newErrors[`uoMId_${d.id}`] = 'Trống';
      if (Number(d.acceptedQuantity) < 0) newErrors[`acceptedQuantity_${d.id}`] = '>= 0';
      if (Number(d.rejectedQuantity) > 0 && !d.rejectReason.trim()) newErrors[`rejectReason_${d.id}`] = 'Nhập lý do';
      
      if (Number(d.acceptedQuantity) > 0 || Number(d.rejectedQuantity) > 0) hasItems = true;
    });

    if (!hasItems) newErrors.details = 'Phải nhập ít nhất 1 mặt hàng có số lượng > 0';

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!validate()) return showToast('warning', 'Kiểm tra lại các dữ liệu còn thiếu!');
    if (!userInfo?.id) return showToast('error', 'Hết phiên đăng nhập!');

    setLoading(true);
    try {
      const safeReceiptDate = new Date(`${receiptDate}T12:00:00Z`).toISOString();

      const payload: InventoryReceiptCreatePayload = {
        warehouseId: warehouseId as number,
        supplierId: supplierId ? (supplierId as number) : undefined,
        receivedById: userInfo.id,
        receiptDate: safeReceiptDate,
        note: notes,
        details: details
          .filter(d => (Number(d.acceptedQuantity) > 0 || Number(d.rejectedQuantity) > 0))
          .map((d) => ({
            variantId: d.variantId as number,
            batchId: d.batchId as number,
            uoMId: d.uoMId as number,
            purchaseOrderDetailId: d.purchaseOrderDetailId || undefined,
            expectedQuantity: Number(d.expectedQuantity),
            acceptedQuantity: Number(d.acceptedQuantity),
            rejectedQuantity: Number(d.rejectedQuantity),
            rejectReason: d.rejectReason.trim() || undefined,
          })),
      };

      await inventoryReceiptApi.create(payload);
      showToast('success', 'NHẬP KHO THÀNH CÔNG!');
      setTimeout(() => navigate('/inventory-receipts'), 1500);
    } catch (error) {
      showToast('error', 'Không thể lưu phiếu nhập. Kiểm tra lại kết nối!');
    } finally {
      setLoading(false);
    }
  };

  return (
    <PageContainer>
      <Toast {...toast} />
      <FormHeader
        title="Phiếu Nhập Kho"
        subtitle="Kiểm đếm thực tế và phân loại Lô/Hạn sử dụng"
        icon={PackageCheck}
        onBack={() => navigate('/inventory-receipts')}
      />

      <form onSubmit={handleSubmit} className="flex flex-col gap-6">
        <FormCard>
          <FormSection title="1. Thông Tin Chung">
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6 mb-4">
              <FormSelect
                label="Kho Lưu Trữ"
                value={warehouseId}
                onSelect={(val) => setWarehouseId(val)}
                options={warehouses}
                required
                error={errors.warehouseId}
              />
              <FormSelect
                label="Nhà Cung Cấp"
                value={supplierId}
                onSelect={(val) => setSupplierId(val)}
                options={suppliers}
                showSearch
                disabled={Boolean(poIdParam)}
              />
              <FormInput
                label="Ngày Nhận Hàng"
                type="date"
                value={receiptDate}
                onChange={(e) => setReceiptDate(e.target.value)}
                required
                error={errors.receiptDate}
              />
            </div>
            <FormTextarea
              label="Ghi chú"
              placeholder="Thông tin thêm về tình trạng xe, bao bì..."
              value={notes}
              onChange={(e: any) => setNotes(e.target.value)}
              rows={2}
            />
          </FormSection>

          <FormSection title="2. Chi Tiết Mặt Hàng">
            {errors.details && <div className="mb-4 text-rose-600 font-bold bg-rose-50 p-3 rounded-lg border border-rose-200">{errors.details}</div>}
            
            <div className="overflow-x-auto border border-slate-200 rounded-xl bg-white shadow-sm mb-4">
              <table className="w-full text-sm text-left">
                <thead className="bg-slate-50 border-b border-slate-200 font-bold text-slate-500 uppercase text-xs">
                  <tr>
                    <th className="px-4 py-3 w-10 text-center">#</th>
                    <th className="px-4 py-3 min-w-50">Sản phẩm</th>
                    <th className="px-4 py-3 min-w-30">ĐVT</th>
                    <th className="px-4 py-3 min-w-55">Lô hàng</th>
                    <th className="px-3 py-3 w-24 text-center bg-slate-100">Dự kiến</th>
                    <th className="px-3 py-3 w-24 text-center bg-emerald-50">Thực nhận</th>
                    <th className="px-3 py-3 w-24 text-center bg-rose-50">Trả về</th>
                    <th className="px-4 py-3 min-w-50">Lý do lỗi</th>
                    <th className="px-4 py-3 w-12 text-center"></th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {details.map((row, index) => {
                    const rowBatches = batches.filter(b => b.variantId === row.variantId);
                    return (
                      <tr key={row.id} className="hover:bg-slate-50/50">
                        <td className="px-4 py-2 text-slate-400 text-center">{index + 1}</td>
                        <td className="p-2">
                          <FormSelect
                            label="" options={variants} value={row.variantId} showSearch placeholder="Chọn SP..."
                            onSelect={(val) => {
                              handleDetailChange(row.id, 'variantId', val);
                              handleDetailChange(row.id, 'batchId', '');
                            }}
                            error={errors[`variantId_${row.id}`]}
                            disabled={Boolean(row.purchaseOrderDetailId)}
                          />
                        </td>
                        <td className="p-2">
                          <FormSelect
                            label="" options={uoms} value={row.uoMId} placeholder="ĐVT"
                            onSelect={(val) => handleDetailChange(row.id, 'uoMId', val)}
                            error={errors[`uoMId_${row.id}`]}
                          />
                        </td>
                        <td className="p-2">
                          <div className="flex gap-1 items-center">
                            <div className="flex-1">
                                <FormSelect
                                    label="" options={rowBatches.map(b => ({ value: b.id, label: b.batchCode }))} 
                                    value={row.batchId} placeholder="Lô..."
                                    onSelect={(val) => handleDetailChange(row.id, 'batchId', val)}
                                    error={errors[`batchId_${row.id}`]}
                                />
                            </div>
                            <button
                              type="button"
                              onClick={() => openBatchModal(row.id, row.variantId)}
                              className="px-2 py-1.5 bg-indigo-50 text-indigo-700 border border-indigo-200 rounded-lg text-[10px] font-black hover:bg-indigo-100 uppercase shrink-0"
                            >
                              + Lô
                            </button>
                          </div>
                        </td>
                        <td className="p-2 bg-slate-50/50 border-l border-slate-100">
                          <FormInput
                            label="" type="number" value={row.expectedQuantity}
                            onChange={(e) => handleDetailChange(row.id, 'expectedQuantity', Number(e.target.value))}
                            className="text-center"
                            disabled={Boolean(row.purchaseOrderDetailId)}
                          />
                        </td>
                        <td className="p-2 bg-emerald-50/20 border-l border-emerald-100">
                          <FormInput
                            label="" type="number" value={row.acceptedQuantity}
                            onChange={(e) => handleDetailChange(row.id, 'acceptedQuantity', Number(e.target.value))}
                            className="text-center font-bold text-emerald-700"
                            error={errors[`acceptedQuantity_${row.id}`]}
                          />
                        </td>
                        <td className="p-2 bg-rose-50/20 border-l border-rose-100">
                          <FormInput
                            label="" type="number" value={row.rejectedQuantity}
                            onChange={(e) => handleDetailChange(row.id, 'rejectedQuantity', Number(e.target.value))}
                            className="text-center font-bold text-rose-700"
                          />
                        </td>
                        <td className="p-2 border-l border-slate-100">
                          <FormInput
                            label="" type="text" placeholder="Lý do..."
                            value={row.rejectReason}
                            onChange={(e) => handleDetailChange(row.id, 'rejectReason', e.target.value)}
                            disabled={Number(row.rejectedQuantity) === 0}
                            error={errors[`rejectReason_${row.id}`]}
                          />
                        </td>
                        <td className="p-2 text-center border-l border-slate-100">
                          <button
                            type="button"
                            onClick={() => handleRemoveRow(row.id)}
                            className="p-2 text-slate-400 hover:text-rose-600 rounded-lg transition-colors disabled:opacity-20"
                            disabled={details.length === 1}
                          >
                            <Trash2 size={16} />
                          </button>
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
              <div className="p-3 bg-slate-50/80 border-t border-slate-200 flex justify-center">
                <button
                  type="button"
                  onClick={handleAddRow}
                  className="flex items-center gap-2 px-4 py-2 text-sm font-bold text-indigo-600 hover:bg-indigo-100 rounded-lg transition-colors"
                >
                  <Plus size={16} /> THÊM DÒNG MỚI
                </button>
              </div>
            </div>
          </FormSection>

          <div className="flex justify-end pt-4 border-t border-slate-100 gap-3">
            <button
              type="button"
              onClick={() => navigate('/inventory-receipts')}
              className="px-6 py-2.5 text-sm font-bold text-slate-600 bg-white border border-slate-300 rounded-xl hover:bg-slate-50 transition-colors shadow-sm"
            >
              Hủy Bỏ
            </button>
            <SubmitButton loading={loading} isEditMode={false} icon={Save} />
          </div>
        </FormCard>
      </form>

      {/* --- BATCH MODAL --- */}
      {showBatchModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/50 backdrop-blur-sm p-4">
          <div className="bg-white rounded-2xl shadow-xl w-full max-w-md overflow-hidden animate-in zoom-in-95 duration-200">
            <div className="flex justify-between items-center px-6 py-4 bg-indigo-50 border-b border-indigo-100">
              <h3 className="text-lg font-black text-indigo-800">Khai Báo Lô Hàng Mới</h3>
              <button onClick={() => setShowBatchModal(false)} className="text-slate-400 hover:text-slate-600 bg-white rounded-full p-1 shadow-sm">
                <X className="w-5 h-5" />
              </button>
            </div>
            
            <div className="p-6 space-y-4">
              <FormInput
                label="Mã Lô (Batch Code) *"
                value={newBatch.batchCode}
                onChange={(e) => setNewBatch({ ...newBatch, batchCode: e.target.value })}
                required
              />
              <FormInput
                label="Ngày Sản Xuất *"
                type="date"
                value={newBatch.manufactureDate}
                onChange={(e) => setNewBatch({ ...newBatch, manufactureDate: e.target.value })}
                required
              />
              <FormInput
                label="Hạn Sử Dụng (Expiry) *"
                type="date"
                value={newBatch.expiryDate}
                onChange={(e) => setNewBatch({ ...newBatch, expiryDate: e.target.value })}
                required
              />
            </div>
            
            <div className="px-6 py-4 bg-slate-50 border-t flex justify-end gap-3">
              <button onClick={() => setShowBatchModal(false)} className="px-5 py-2 text-sm font-bold text-slate-600 bg-white border border-slate-200 rounded-lg hover:bg-slate-100 shadow-sm">
                Hủy Bỏ
              </button>
              <button onClick={handleSaveBatch} className="flex items-center px-5 py-2 text-sm font-bold text-white bg-indigo-600 rounded-lg hover:bg-indigo-700 shadow-sm">
                <Save className="w-4 h-4 mr-2" /> Lưu Lô Mới
              </button>
            </div>
          </div>
        </div>
      )}
    </PageContainer>
  );
};

export default InventoryReceiptForm;
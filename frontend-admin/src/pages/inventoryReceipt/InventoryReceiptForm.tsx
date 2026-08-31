import React, { useState, useEffect, useCallback } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { Plus, Trash2, Save, PackageCheck } from 'lucide-react';

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
import DatePicker from '../../components/commons/CustomDatePicker';
import { Toast } from '../../components/commons/Toast';
import { ModalCreateBatch } from '../../components/modals/ModalCreateBatch';

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
import { PurchaseOrder, PurchaseOrderStatus } from '../../types/purchaseOrder';

// Types cho Dropdown
interface SelectOption {
  value: number;
  label: string;
}

interface DetailRow {
  id: string;
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
  const [toast, setToast] = useState<{
    show: boolean;
    type: 'success' | 'warning' | 'error';
    message: string;
  }>({ show: false, type: 'success', message: '' });

  // --- DROPDOWN OPTIONS ---
  const [warehouses, setWarehouses] = useState<SelectOption[]>([]);
  const [suppliers, setSuppliers] = useState<SelectOption[]>([]);
  const [variants, setVariants] = useState<SelectOption[]>([]);
  const [rawVariants, setRawVariants] = useState<any[]>([]);
  const [uoms, setUoms] = useState<SelectOption[]>([]);
  const [batches, setBatches] = useState<{ id: number; variantId: number; batchCode: string }[]>(
    []
  );
  const [purchaseOrders, setPurchaseOrders] = useState<
    { value: number; label: string; data: PurchaseOrder }[]
  >([]);

  // --- FORM STATES ---
  const [selectedPoId, setSelectedPoId] = useState<number | ''>('');
  const [warehouseId, setWarehouseId] = useState<number | ''>('');
  const [supplierId, setSupplierId] = useState<number | ''>('');
  const [receiptDate, setReceiptDate] = useState<Date | null>(new Date());
  const [notes, setNotes] = useState('');

  const [details, setDetails] = useState<DetailRow[]>([
    {
      id: crypto.randomUUID(),
      variantId: '',
      batchId: '',
      uoMId: '',
      expectedQuantity: 0,
      acceptedQuantity: 0,
      rejectedQuantity: 0,
      rejectReason: '',
    },
  ]);
  const [errors, setErrors] = useState<Record<string, string>>({});

  // --- BATCH MODAL STATES ---
  const [showBatchModal, setShowBatchModal] = useState(false);
  const [batchModalRowId, setBatchModalRowId] = useState('');
  const [batchModalVariant, setBatchModalVariant] = useState<{
    id: number;
    code?: string;
    name?: string;
  } | null>(null);

  const showToast = (type: 'success' | 'warning' | 'error', message: string) => {
    setToast({ show: true, type, message });
    setTimeout(() => setToast((prev) => ({ ...prev, show: false })), 3000);
  };

  // --- EFFECTS ---
  const loadOptions = useCallback(async () => {
    try {
      const [whRes, supRes, varRes, uomRes, poRes] = await Promise.all([
        warehouseApi.getAllList().catch(() => []),
        supplierApi.getAllList().catch(() => []),
        productVariantApi.getAllList().catch(() => []),
        uomApi.getAllList().catch(() => []),
        purchaseOrderApi.getAllList?.().catch(() => []) || Promise.resolve([]),
      ]);

      let batchRes: any[] = [];
      try {
        const bData = await productBatchApi.getAllList();
        batchRes = bData || [];
      } catch (e) {
        console.warn('API ProductBatch chưa sẵn sàng hoặc rỗng.');
      }

      setWarehouses(whRes.map((w: any) => ({ value: w.id, label: w.name })));
      setSuppliers(supRes.map((s: any) => ({ value: s.id, label: s.name })));
      setRawVariants(varRes);
      setVariants(varRes.map((v: any) => ({ value: v.id, label: `${v.code} - ${v.name}` })));
      setUoms(uomRes.map((u: any) => ({ value: u.id, label: u.name })));
      setBatches(
        batchRes.map((b) => ({ id: b.id, variantId: b.variantId, batchCode: b.batchCode }))
      );

      // Lọc các Đơn Mua Hàng đã duyệt hoặc đang giao từng phần
      const validPOs = (poRes || []).filter(
        (p: PurchaseOrder) =>
          p.status === PurchaseOrderStatus.Approved ||
          p.status === PurchaseOrderStatus.PartiallyReceived
      );
      setPurchaseOrders(
        validPOs.map((p: PurchaseOrder) => ({
          value: p.id,
          label: `${p.orderCode} - ${p.supplierName || 'NCC'} (${(p.totalAmount || 0).toLocaleString('vi-VN')} ₫)`,
          data: p,
        }))
      );
    } catch (error) {
      showToast('error', 'Lỗi hệ thống khi tải danh mục bổ trợ!');
    }
  }, []);

  const loadPurchaseOrder = useCallback(
    async (id: number) => {
      try {
        const data = await purchaseOrderApi.getById(id);
        if (data) {
          setSelectedPoId(data.id);
          setSupplierId(data.supplierId || '');

          // Tự động tìm và gán kho lưu trữ nếu có trong ghi chú PO
          if (data.note) {
            const match = data.note.match(/\[Kho nhận:\s*([^\]]+)\]/i);
            if (match) {
              const whSearch = match[1].trim();
              const found = warehouses.find(
                (w) => whSearch.includes(w.label) || w.label.includes(whSearch)
              );
              if (found) {
                setWarehouseId(found.value);
              }
            }
          }

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
            showToast('success', `Đã tải ${loadedDetails.length} mặt hàng từ đơn mua ${data.orderCode}!`);
          }
        }
      } catch (error) {
        showToast('error', 'Không thể tải thông tin Đơn mua hàng gốc');
      }
    },
    [warehouses]
  );

  useEffect(() => {
    loadOptions();
  }, [loadOptions]);

  useEffect(() => {
    if (poIdParam) {
      loadPurchaseOrder(parseInt(poIdParam));
    }
  }, [poIdParam, loadPurchaseOrder]);

  // Khi người dùng chọn PO từ Dropdown
  const handleSelectPO = (poId: number | '') => {
    if (!poId || poId === 0) {
      setSelectedPoId('');
      setSupplierId('');
      setDetails([
        {
          id: crypto.randomUUID(),
          variantId: '',
          batchId: '',
          uoMId: '',
          expectedQuantity: 0,
          acceptedQuantity: 0,
          rejectedQuantity: 0,
          rejectReason: '',
        },
      ]);
      return;
    }
    loadPurchaseOrder(Number(poId));
  };

  // --- HANDLERS ---
  const handleAddRow = () => {
    setDetails([
      ...details,
      {
        id: crypto.randomUUID(),
        variantId: '',
        batchId: '',
        uoMId: '',
        expectedQuantity: 0,
        acceptedQuantity: 0,
        rejectedQuantity: 0,
        rejectReason: '',
      },
    ]);
  };

  const handleRemoveRow = (id: string) => {
    if (details.length > 1) {
      setDetails(details.filter((d) => d.id !== id));
    }
  };

  const handleDetailChange = (id: string, field: keyof DetailRow, value: any) => {
    setDetails((prev) =>
      prev.map((d) => {
        if (d.id === id) {
          const updated = { ...d, [field]: value };

          if (field === 'rejectedQuantity' && Number(value) === 0) {
            updated.rejectReason = '';
          }
          return updated;
        }
        return d;
      })
    );

    if (errors[`${field}_${id}`]) {
      setErrors((prev) => {
        const newErr = { ...prev };
        delete newErr[`${field}_${id}`];
        return newErr;
      });
    }
  };

  // --- BATCH (LÔ HÀNG) HANDLERS ---
  const openBatchModal = (rowId: string, variantId: number | '') => {
    if (!variantId) {
      showToast('warning', 'Vui lòng chọn Sản phẩm trước khi tạo Lô mới!');
      return;
    }
    if (!supplierId) {
      showToast('warning', 'Vui lòng chọn Nhà Cung Cấp hoặc Đơn Mua Hàng trước khi tạo Lô!');
      return;
    }
    const variantObj = rawVariants.find((v) => v.id === Number(variantId));
    setBatchModalRowId(rowId);
    setBatchModalVariant(
      variantObj
        ? { id: variantObj.id, code: variantObj.code, name: variantObj.name }
        : { id: Number(variantId) }
    );
    setShowBatchModal(true);
  };

  const handleBatchCreated = (newBatch: { id: number; variantId: number; batchCode: string }) => {
    setBatches((prev) => [...prev, newBatch]);
    handleDetailChange(batchModalRowId, 'batchId', newBatch.id);
    showToast('success', `Đã tạo thành công Lô ${newBatch.batchCode}!`);
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
      if (Number(d.rejectedQuantity) > 0 && !d.rejectReason.trim())
        newErrors[`rejectReason_${d.id}`] = 'Nhập lý do';

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
      const safeReceiptDate = receiptDate ? receiptDate.toISOString() : new Date().toISOString();

      const payload: InventoryReceiptCreatePayload = {
        warehouseId: warehouseId as number,
        supplierId: supplierId ? (supplierId as number) : undefined,
        receivedById: userInfo.id,
        receiptDate: safeReceiptDate,
        note: notes,
        details: details
          .filter((d) => Number(d.acceptedQuantity) > 0 || Number(d.rejectedQuantity) > 0)
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
      setTimeout(() => navigate('/inventory-receipts'), 1200);
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
        subtitle="Kiểm đếm thực tế hàng giao tại cửa kho và phân loại Lô/Hạn sử dụng (FEFO)"
        icon={PackageCheck}
        onBack={() => navigate('/inventory-receipts')}
      />

      <form onSubmit={handleSubmit} className="flex flex-col gap-6">
        <FormCard>
          {/* --- SECTION 1: THÔNG TIN CHUNG --- */}
          <FormSection title="1. Thông Tin Chung">
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 mb-4">
              {/* CHỌN ĐƠN MUA HÀNG (PO) - AUTO FILL TOÀN BỘ */}
              <div className="lg:col-span-2">
                <FormSelect
                  label="Đơn Mua Hàng (PO) Tham Chiếu"
                  value={selectedPoId}
                  onSelect={(val) => handleSelectPO(val)}
                  options={[
                    { value: 0, label: '-- Nhập tự do (Không theo đơn PO) --' },
                    ...purchaseOrders,
                  ]}
                  showSearch
                  searchPlaceholder="Tìm mã đơn PO hoặc Nhà cung cấp..."
                  placeholder="-- Chọn Đơn Mua Hàng PO để tự động điền --"
                />
              </div>

              <FormSelect
                label="Kho Lưu Trữ"
                value={warehouseId}
                onSelect={(val) => {
                  setWarehouseId(val);
                  setErrors((prev) => ({ ...prev, warehouseId: '' }));
                }}
                options={warehouses}
                required
                error={errors.warehouseId}
                showSearch
                searchPlaceholder="Tìm kho..."
                placeholder="-- Chọn kho nhận hàng --"
              />

              <FormSelect
                label="Nhà Cung Cấp"
                value={supplierId}
                onSelect={(val) => setSupplierId(val)}
                options={suppliers}
                showSearch
                searchPlaceholder="Tìm NCC..."
                placeholder="-- Chọn Nhà cung cấp --"
                disabled={Boolean(selectedPoId && selectedPoId !== 0)}
              />

              <div className="lg:col-span-2">
                <DatePicker
                  label="Ngày Nhận Hàng"
                  required
                  value={receiptDate}
                  onChange={(date) => {
                    setReceiptDate(date);
                    setErrors((prev) => ({ ...prev, receiptDate: '' }));
                  }}
                  error={errors.receiptDate}
                  placeholder="Chọn ngày nhận hàng..."
                />
              </div>

              <div className="lg:col-span-2">
                <FormTextarea
                  label="Ghi chú đợt nhận hàng"
                  placeholder="Tình trạng xe tải bảo ôn, độ tươi nông sản, bao bì đóng gói..."
                  value={notes}
                  onChange={(e: any) => setNotes(e.target.value)}
                  rows={2}
                />
              </div>
            </div>
          </FormSection>

          {/* --- SECTION 2: CHI TIẾT MẶT HÀNG --- */}
          <FormSection title="2. Chi Tiết Mặt Hàng">
            {errors.details && (
              <div className="mb-4 text-rose-600 font-bold bg-rose-50 p-3.5 rounded-xl border border-rose-200 text-xs">
                {errors.details}
              </div>
            )}

            <div className="overflow-x-auto border border-slate-200 rounded-2xl bg-white shadow-2xs mb-4 min-h-[320px]">
              <table className="w-full text-sm text-left border-collapse min-w-[760px]">
                <thead className="bg-slate-50/80 border-b border-slate-200 font-bold text-slate-500 uppercase text-xs">
                  <tr>
                    <th className="px-3 py-3.5 w-10 text-center">#</th>
                    <th className="px-3 py-3.5 w-[30%] min-w-[170px]">
                      Sản phẩm <span className="text-red-500">*</span>
                    </th>
                    <th className="px-2 py-3.5 w-24 min-w-[85px]">
                      ĐVT <span className="text-red-500">*</span>
                    </th>
                    <th className="px-3 py-3.5 w-[36%] min-w-[220px]">
                      Lô hàng (FEFO) <span className="text-red-500">*</span>
                    </th>
                    <th className="px-2 py-3.5 w-20 text-center bg-slate-100/70 whitespace-nowrap">
                      Dự kiến
                    </th>
                    <th className="px-2 py-3.5 w-20 text-center bg-emerald-50/70 text-emerald-800 whitespace-nowrap">
                      Thực nhận <span className="text-red-500">*</span>
                    </th>
                    <th className="px-2 py-3.5 w-18 text-center bg-rose-50/70 text-rose-800 whitespace-nowrap">
                      Trả về
                    </th>
                    <th className="px-3 py-3.5 w-36 min-w-[120px] whitespace-nowrap">
                      Lý do lỗi
                    </th>
                    <th className="px-2 py-3.5 w-10 text-center"></th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {details.map((row, index) => {
                    const rowBatches = batches.filter((b) => b.variantId === row.variantId);
                    return (
                      <tr
                        key={row.id}
                        className="hover:bg-slate-50/60 transition-colors"
                        style={{ zIndex: 50 - index }}
                      >
                        <td className="px-3 py-3 text-slate-400 text-center font-medium">
                          {index + 1}
                        </td>
                        <td className="p-2">
                          <FormSelect
                            label=""
                            options={variants}
                            value={row.variantId}
                            showSearch
                            searchPlaceholder="Tìm sản phẩm..."
                            placeholder="Chọn SP..."
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
                            label=""
                            options={uoms}
                            value={row.uoMId}
                            placeholder="ĐVT"
                            onSelect={(val) => handleDetailChange(row.id, 'uoMId', val)}
                            error={errors[`uoMId_${row.id}`]}
                            disabled={Boolean(row.purchaseOrderDetailId)}
                          />
                        </td>
                        <td className="p-2">
                          <div className="flex gap-1.5 items-center">
                            <div className="flex-1 min-w-0">
                              <FormSelect
                                label=""
                                options={rowBatches.map((b) => ({
                                  value: b.id,
                                  label: b.batchCode,
                                }))}
                                value={row.batchId}
                                placeholder="Lô..."
                                showSearch
                                searchPlaceholder="Tìm mã lô..."
                                onSelect={(val) => handleDetailChange(row.id, 'batchId', val)}
                                error={errors[`batchId_${row.id}`]}
                              />
                            </div>
                            {!row.batchId && (
                              <button
                                type="button"
                                onClick={() => openBatchModal(row.id, row.variantId)}
                                className="px-2.5 py-2.5 bg-amber-50 text-amber-800 border border-amber-200 hover:bg-amber-100 rounded-xl text-xs font-bold transition-all shadow-2xs shrink-0 cursor-pointer flex items-center gap-1"
                                title="Tạo Lô Hàng Mới Cho Mặt Hàng Này"
                              >
                                + Lô
                              </button>
                            )}
                          </div>
                        </td>
                        <td className="p-2 bg-slate-50/50 border-l border-slate-100 text-center">
                          <input
                            type="number"
                            min="0"
                            value={row.expectedQuantity}
                            onFocus={(e) => e.target.select()}
                            onChange={(e) => {
                              const val = e.target.value === '' ? 0 : Math.max(0, parseInt(e.target.value, 10) || 0);
                              handleDetailChange(row.id, 'expectedQuantity', val);
                            }}
                            className="w-16 h-10 mx-auto block text-center font-bold text-slate-600 bg-slate-100 border border-slate-200 rounded-xl text-sm outline-none"
                            disabled={Boolean(row.purchaseOrderDetailId)}
                          />
                        </td>
                        <td className="p-2 bg-emerald-50/20 border-l border-emerald-100 text-center">
                          <input
                            type="number"
                            min="0"
                            value={row.acceptedQuantity}
                            onFocus={(e) => e.target.select()}
                            onChange={(e) => {
                              const val = e.target.value === '' ? 0 : Math.max(0, parseInt(e.target.value, 10) || 0);
                              handleDetailChange(row.id, 'acceptedQuantity', val);
                            }}
                            className="w-16 h-10 mx-auto block text-center font-black text-emerald-700 bg-white border border-emerald-300 rounded-xl text-sm focus:ring-2 focus:ring-emerald-400 outline-none shadow-2xs"
                          />
                        </td>
                        <td className="p-2 bg-rose-50/20 border-l border-rose-100 text-center">
                          <input
                            type="number"
                            min="0"
                            value={row.rejectedQuantity}
                            onFocus={(e) => e.target.select()}
                            onChange={(e) => {
                              const val = e.target.value === '' ? 0 : Math.max(0, parseInt(e.target.value, 10) || 0);
                              handleDetailChange(row.id, 'rejectedQuantity', val);
                            }}
                            className="w-16 h-10 mx-auto block text-center font-bold text-rose-700 bg-white border border-rose-300 rounded-xl text-sm focus:ring-2 focus:ring-rose-400 outline-none shadow-2xs"
                          />
                        </td>
                        <td className="p-2 border-l border-slate-100">
                          <FormInput
                            label=""
                            type="text"
                            placeholder="Lý do..."
                            value={row.rejectReason}
                            onChange={(e) =>
                              handleDetailChange(row.id, 'rejectReason', e.target.value)
                            }
                            disabled={Number(row.rejectedQuantity) === 0}
                            error={errors[`rejectReason_${row.id}`]}
                          />
                        </td>
                        <td className="p-2 text-center border-l border-slate-100">
                          <button
                            type="button"
                            onClick={() => handleRemoveRow(row.id)}
                            className="p-2 text-slate-400 hover:text-rose-600 hover:bg-rose-50 rounded-xl transition-colors disabled:opacity-20 cursor-pointer"
                            disabled={details.length === 1}
                            title="Xóa dòng"
                          >
                            <Trash2 size={16} />
                          </button>
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
              <div className="p-3.5 bg-slate-50/50 border-t border-slate-200 flex justify-center">
                <button
                  type="button"
                  onClick={handleAddRow}
                  className="flex items-center gap-2 px-5 py-2.5 text-sm font-bold text-amber-700 bg-amber-50 hover:bg-amber-100 border border-amber-200 rounded-xl transition-all cursor-pointer shadow-2xs hover:shadow-xs active:scale-98"
                >
                  <Plus size={16} strokeWidth={3} /> THÊM DÒNG MỚI
                </button>
              </div>
            </div>
          </FormSection>

          <div className="flex justify-end pt-4 border-t border-slate-100 gap-3">
            <button
              type="button"
              onClick={() => navigate('/inventory-receipts')}
              className="px-6 py-2.5 text-sm font-bold text-slate-600 bg-white border border-slate-200 rounded-xl hover:bg-slate-50 transition-colors shadow-2xs cursor-pointer"
            >
              Hủy Bỏ
            </button>
            <SubmitButton loading={loading} isEditMode={false} icon={Save} />
          </div>
        </FormCard>
      </form>

      {/* --- STANDALONE BATCH MODAL --- */}
      <ModalCreateBatch
        isOpen={showBatchModal}
        onClose={() => setShowBatchModal(false)}
        onSuccess={handleBatchCreated}
        variantId={batchModalVariant?.id || 0}
        variantCode={batchModalVariant?.code}
        variantName={batchModalVariant?.name}
        supplierId={Number(supplierId) || 0}
      />
    </PageContainer>
  );
};

export default InventoryReceiptForm;

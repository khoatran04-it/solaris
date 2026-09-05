import React, { useEffect, useState, useMemo } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import {
  Settings2,
  Save,
  CheckCircle2,
  Search,
  SlidersHorizontal,
  Info,
  Check,
  Asterisk,
  Layers,
  Sparkles,
} from 'lucide-react';

// API & Types
import { categoryAttributeApi } from '../../api/categoryAttributeApi';
import { productCategoryApi } from '../../api/productCategoryApi';
import { attributeDefinitionApi } from '../../api/attributeDefinitionApi';
import { ProductCategory } from '../../types/productCategory';
import { AttributeDefinition } from '../../types/attributeDefinition';

// Shared UI Components
import { Toast } from '../../components/commons/Toast';
import {
  PageContainer,
  FormCard,
  FormHeader,
  FormSection,
  FormSelect,
  SubmitButton,
} from '../../components/commons/FormUI';

interface AttributeSelectionState {
  isSelected: boolean;
  isRequired: boolean;
}

const CategoryAttributeForm: React.FC = () => {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const initialCatId = searchParams.get('categoryId') ? Number(searchParams.get('categoryId')) : 0;

  // --- STATES ---
  const [selectedCategoryId, setSelectedCategoryId] = useState<number>(initialCatId);
  const [categories, setCategories] = useState<ProductCategory[]>([]);
  const [attributes, setAttributes] = useState<AttributeDefinition[]>([]);
  
  // Map lưu trạng thái chọn của từng AttributeId: { [attributeId]: { isSelected: boolean, isRequired: boolean } }
  const [selectedMap, setSelectedMap] = useState<Record<number, AttributeSelectionState>>({});

  // Filter & Search trong Matrix
  const [searchKeyword, setSearchKeyword] = useState<string>('');
  const [selectedTypeFilter, setSelectedTypeFilter] = useState<string>('ALL');

  const [loading, setLoading] = useState(false);
  const [fetchingExisting, setFetchingExisting] = useState(false);
  const [toast, setToast] = useState<{
    show: boolean;
    type: 'success' | 'warning' | 'error';
    message: string;
  }>({
    show: false,
    type: 'success',
    message: '',
  });

  // --- EFFECTS ---
  useEffect(() => {
    // 1. Tải danh sách Danh mục và Từ điển Thuộc tính
    Promise.all([productCategoryApi.getAllList(), attributeDefinitionApi.getAllList()])
      .then(([cats, attrs]) => {
        setCategories(cats);
        setAttributes(attrs);
      })
      .catch(() => showToast('error', 'Không thể tải dữ liệu danh mục và thuộc tính!'));
  }, []);

  // Khi chọn một Category -> Tải các thuộc tính đã được cấu hình trước đó của danh mục này
  useEffect(() => {
    if (!selectedCategoryId || selectedCategoryId === 0) {
      setSelectedMap({});
      return;
    }

    setFetchingExisting(true);
    categoryAttributeApi
      .getByCategoryId(selectedCategoryId)
      .then((existingList) => {
        const newMap: Record<number, AttributeSelectionState> = {};
        existingList.forEach((item) => {
          if (item.attributeDefinitionId) {
            newMap[item.attributeDefinitionId] = {
              isSelected: true,
              isRequired: item.isRequired,
            };
          }
        });
        setSelectedMap(newMap);
      })
      .catch(() => {
        showToast('warning', 'Không thể tải cấu hình hiện tại của danh mục');
      })
      .finally(() => {
        setFetchingExisting(false);
      });
  }, [selectedCategoryId]);

  // --- HELPERS ---
  const showToast = (type: 'success' | 'warning' | 'error', message: string) => {
    setToast({ show: true, type, message });
    setTimeout(() => setToast((prev) => ({ ...prev, show: false })), 3000);
  };

  // Toggle chọn / bỏ chọn một thuộc tính
  const handleToggleSelect = (attrId: number) => {
    setSelectedMap((prev) => {
      const current = prev[attrId];
      if (current?.isSelected) {
        // Bỏ chọn
        const updated = { ...prev };
        delete updated[attrId];
        return updated;
      } else {
        // Chọn mới (mặc định isRequired = false)
        return {
          ...prev,
          [attrId]: {
            isSelected: true,
            isRequired: false,
          },
        };
      }
    });
  };

  // Đổi trạng thái Bắt buộc / Tùy chọn
  const handleToggleRequired = (attrId: number, e: React.MouseEvent) => {
    e.stopPropagation(); // Không trigger toggle card
    setSelectedMap((prev) => {
      const current = prev[attrId];
      if (!current) return prev;
      return {
        ...prev,
        [attrId]: {
          ...current,
          isRequired: !current.isRequired,
        },
      };
    });
  };

  // Chọn tất cả các thuộc tính đang hiển thị
  const handleSelectAll = () => {
    const updated = { ...selectedMap };
    filteredAttributes.forEach((attr) => {
      if (!updated[attr.id]?.isSelected) {
        updated[attr.id] = { isSelected: true, isRequired: false };
      }
    });
    setSelectedMap(updated);
  };

  // Bỏ chọn tất cả
  const handleDeselectAll = () => {
    setSelectedMap({});
  };

  // --- FILTERED ATTRIBUTES ---
  const filteredAttributes = useMemo(() => {
    return attributes.filter((attr) => {
      const matchKeyword =
        !searchKeyword ||
        attr.name.toLowerCase().includes(searchKeyword.toLowerCase()) ||
        attr.code?.toLowerCase().includes(searchKeyword.toLowerCase());

      const matchType =
        selectedTypeFilter === 'ALL' ||
        (selectedTypeFilter === 'TEXT' && (!attr.dataType || attr.dataType.toLowerCase().includes('text') || attr.dataType.toLowerCase().includes('string') || attr.dataType.toLowerCase().includes('văn bản'))) ||
        (selectedTypeFilter === 'NUMBER' && (attr.dataType?.toLowerCase().includes('number') || attr.dataType?.toLowerCase().includes('số') || attr.dataType?.toLowerCase().includes('int') || attr.dataType?.toLowerCase().includes('decimal'))) ||
        (selectedTypeFilter === 'SELECT' && (attr.dataType?.toLowerCase().includes('select') || attr.dataType?.toLowerCase().includes('option') || attr.dataType?.toLowerCase().includes('lựa chọn')));

      return matchKeyword && matchType;
    });
  }, [attributes, searchKeyword, selectedTypeFilter]);

  const selectedCount = Object.values(selectedMap).filter((item) => item.isSelected).length;

  // --- SUBMIT BULK SYNC ---
  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedCategoryId || selectedCategoryId === 0) {
      return showToast('warning', 'Vui lòng chọn danh mục sản phẩm trước khi lưu!');
    }

    const payloadAttributes = Object.entries(selectedMap)
      .filter(([_, val]) => val.isSelected)
      .map(([attrIdStr, val]) => ({
        attributeDefinitionId: Number(attrIdStr),
        isRequired: val.isRequired,
      }));

    setLoading(true);
    try {
      await categoryAttributeApi.sync({
        categoryId: selectedCategoryId,
        attributes: payloadAttributes,
      });

      showToast('success', `ĐÃ ĐỒNG BỘ ${payloadAttributes.length} THUỘC TÍNH CHO DANH MỤC THÀNH CÔNG!`);
      setTimeout(() => navigate('/category-attributes'), 1200);
    } catch (error: any) {
      const errorMessage = error.response?.data?.message || 'Có lỗi xảy ra khi lưu cấu hình.';
      showToast('error', errorMessage);
    } finally {
      setLoading(false);
    }
  };

  // Helper render Badge kiểu dữ liệu
  const renderDataTypeBadge = (dataType?: string) => {
    const type = (dataType || 'text').toLowerCase();
    if (type.includes('num') || type.includes('số') || type.includes('int') || type.includes('decimal')) {
      return (
        <span className="px-2 py-0.5 rounded-md text-[11px] font-bold bg-purple-50 text-purple-700 border border-purple-200">
          Số (Number)
        </span>
      );
    }
    if (type.includes('select') || type.includes('option') || type.includes('chọn')) {
      return (
        <span className="px-2 py-0.5 rounded-md text-[11px] font-bold bg-emerald-50 text-emerald-700 border border-emerald-200">
          Lựa chọn (Select)
        </span>
      );
    }
    return (
      <span className="px-2 py-0.5 rounded-md text-[11px] font-bold bg-blue-50 text-blue-700 border border-blue-200">
        Văn bản (Text)
      </span>
    );
  };

  return (
    <PageContainer>
      <Toast {...toast} />

      <FormHeader
        title="Ma Trận Cấu Hình Thuộc Tính Danh Mục"
        subtitle="Chọn một danh mục sản phẩm và tích chọn toàn bộ các thuộc tính cần áp dụng trong 1 lần duy nhất"
        onBack={() => navigate('/category-attributes')}
        icon={Settings2}
      />

      <FormCard>
        <form onSubmit={handleSubmit} className="flex flex-col gap-8">
          {/* PHẦN 1: CHỌN DANH MỤC SẢN PHẨM */}
          <FormSection title="1. Chọn Danh Mục Sản Phẩm Áp Dụng">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6 items-center">
              <FormSelect
                label="Danh mục sản phẩm"
                required
                showSearch
                searchPlaceholder="Tìm danh mục sản phẩm..."
                placeholder="-- Chọn danh mục sản phẩm để cấu hình --"
                value={selectedCategoryId}
                options={[
                  { label: '-- Chọn danh mục sản phẩm để cấu hình --', value: 0 },
                  ...categories.map((c) => ({
                    label: `${c.name}${c.groupName ? ` (${c.groupName})` : ''}`,
                    value: c.id,
                  })),
                ]}
                onSelect={(val) => setSelectedCategoryId(Number(val))}
              />

              {selectedCategoryId > 0 && (
                <div className="flex items-center gap-3 p-3.5 bg-yellow-50/70 border border-yellow-200/60 rounded-xl">
                  <Sparkles className="w-5 h-5 text-yellow-600 shrink-0" />
                  <div className="text-xs text-yellow-900 leading-relaxed">
                    Đang thiết lập cho: <strong className="font-bold">{categories.find((c) => c.id === selectedCategoryId)?.name}</strong>
                    <span className="block text-yellow-700 mt-0.5">
                      Đã chọn: <strong>{selectedCount}</strong> thuộc tính (trong đó <strong>{Object.values(selectedMap).filter((v) => v.isSelected && v.isRequired).length}</strong> thuộc tính bắt buộc).
                    </span>
                  </div>
                </div>
              )}
            </div>
          </FormSection>

          {/* PHẦN 2: DANH SÁCH THUỘC TÍNH (GRID MATRIX) */}
          <FormSection title="2. Tích Chọn Thuộc Tính Từ Từ Điển Hệ Thống">
            {!selectedCategoryId || selectedCategoryId === 0 ? (
              <div className="py-12 px-6 flex flex-col items-center justify-center text-center bg-slate-50/50 border border-dashed border-slate-200 rounded-2xl">
                <Layers className="w-12 h-12 text-slate-300 mb-3" />
                <h4 className="text-sm font-bold text-slate-700">Chưa chọn danh mục sản phẩm</h4>
                <p className="text-xs text-slate-400 max-w-md mt-1">
                  Vui lòng chọn một danh mục sản phẩm ở phía trên để mở bảng ma trận gán thuộc tính.
                </p>
              </div>
            ) : fetchingExisting ? (
              <div className="py-12 flex items-center justify-center text-slate-400 text-sm font-medium">
                Đang tải cấu hình thuộc tính của danh mục...
              </div>
            ) : (
              <div className="flex flex-col gap-5">
                {/* TOOLBAR TÌM KIẾM & NÚT CHỌN NHANH */}
                <div className="flex flex-wrap items-center justify-between gap-3 p-3 bg-slate-50 rounded-xl border border-slate-200/60">
                  <div className="flex items-center gap-2 flex-1 min-w-[240px]">
                    <div className="relative w-full max-w-xs">
                      <Search className="w-4 h-4 text-slate-400 absolute left-3 top-1/2 -translate-y-1/2" />
                      <input
                        type="text"
                        placeholder="Tìm thuộc tính..."
                        value={searchKeyword}
                        onChange={(e) => setSearchKeyword(e.target.value)}
                        className="w-full pl-9 pr-3 py-1.5 bg-white border border-slate-200 rounded-lg text-xs font-medium text-slate-700 placeholder-slate-400 focus:outline-none focus:border-yellow-400 transition-all"
                      />
                    </div>
                  </div>

                  <div className="flex items-center gap-2">
                    <button
                      type="button"
                      onClick={handleSelectAll}
                      className="px-3 py-1.5 text-xs font-bold text-slate-700 bg-white border border-slate-200 hover:bg-slate-100 rounded-lg transition-colors cursor-pointer"
                    >
                      Chọn tất cả ({filteredAttributes.length})
                    </button>
                    <button
                      type="button"
                      onClick={handleDeselectAll}
                      className="px-3 py-1.5 text-xs font-bold text-slate-500 bg-white border border-slate-200 hover:bg-slate-100 rounded-lg transition-colors cursor-pointer"
                    >
                      Bỏ chọn hết
                    </button>
                  </div>
                </div>

                {/* MATRIX GRID CARDS */}
                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                  {filteredAttributes.map((attr) => {
                    const state = selectedMap[attr.id];
                    const isSelected = Boolean(state?.isSelected);
                    const isRequired = Boolean(state?.isRequired);

                    return (
                      <div
                        key={attr.id}
                        onClick={() => handleToggleSelect(attr.id)}
                        className={`relative p-4.5 rounded-2xl border transition-all duration-200 flex flex-col justify-between gap-4 cursor-pointer select-none ${
                          isSelected
                            ? 'bg-amber-50/40 border-yellow-400 shadow-sm shadow-yellow-100/50'
                            : 'bg-white border-slate-200 hover:border-slate-300 hover:shadow-xs'
                        }`}
                      >
                        {/* Header của Card */}
                        <div className="flex items-start justify-between gap-3">
                          <div className="flex items-start gap-3">
                            <div
                              className={`w-5 h-5 rounded-md flex items-center justify-center mt-0.5 transition-colors ${
                                isSelected
                                  ? 'bg-yellow-400 text-slate-900 shadow-xs'
                                  : 'border-2 border-slate-300 bg-white'
                              }`}
                            >
                              {isSelected && <Check className="w-3.5 h-3.5 stroke-[3]" />}
                            </div>
                            <div>
                              <h4
                                className={`text-sm font-bold leading-tight ${
                                  isSelected ? 'text-slate-900' : 'text-slate-700'
                                }`}
                              >
                                {attr.name}
                              </h4>
                              {attr.code && (
                                <span className="text-[11px] font-semibold text-slate-400 mt-0.5 block">
                                  Mã: {attr.code}
                                </span>
                              )}
                            </div>
                          </div>

                          {renderDataTypeBadge(attr.dataType)}
                        </div>

                        {/* Footer của Card: Toggle Bắt buộc / Tùy chọn khi được chọn */}
                        {isSelected && (
                          <div className="pt-3 border-t border-yellow-200/60 flex items-center justify-between">
                            <span className="text-xs font-semibold text-slate-600">
                              Yêu cầu nhập liệu:
                            </span>

                            <button
                              type="button"
                              onClick={(e) => handleToggleRequired(attr.id, e)}
                              className={`px-2.5 py-1 rounded-lg text-xs font-bold flex items-center gap-1 transition-all cursor-pointer ${
                                isRequired
                                  ? 'bg-rose-100 text-rose-700 border border-rose-200'
                                  : 'bg-slate-100 text-slate-600 hover:bg-slate-200'
                              }`}
                            >
                              {isRequired ? (
                                <>
                                  <Asterisk className="w-3 h-3 text-rose-600 stroke-[3]" />
                                  Bắt buộc
                                </>
                              ) : (
                                'Tùy chọn'
                              )}
                            </button>
                          </div>
                        )}
                      </div>
                    );
                  })}
                </div>

                {filteredAttributes.length === 0 && (
                  <div className="py-8 text-center text-slate-400 text-xs font-medium">
                    Không tìm thấy thuộc tính nào phù hợp với bộ lọc.
                  </div>
                )}
              </div>
            )}
          </FormSection>

          {/* FOOTER NÚT LƯU THAY ĐỔI */}
          <div className="flex items-center justify-between pt-6 border-t border-slate-100 mt-2">
            <button
              type="button"
              onClick={() => navigate('/category-attributes')}
              className="px-5 py-2.5 text-sm font-bold text-slate-600 hover:bg-slate-100 rounded-xl transition-colors cursor-pointer"
            >
              Hủy Bỏ
            </button>

            <SubmitButton
              loading={loading}
              isEditMode={true}
              disabled={selectedCategoryId === 0}
              label={
                selectedCount > 0
                  ? `Lưu Ma Trận (${selectedCount} thuộc tính)`
                  : 'Lưu Cấu Hình'
              }
              icon={Save}
            />
          </div>
        </form>
      </FormCard>
    </PageContainer>
  );
};

export default CategoryAttributeForm;

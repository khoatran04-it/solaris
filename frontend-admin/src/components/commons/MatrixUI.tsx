import React from 'react';
import { ShieldCheck, CheckSquare, Square } from 'lucide-react';

export interface PermissionDef {
  id: number;
  module: string;
  code: string;
  name: string;
}

interface MatrixUIProps {
  allPermissions: PermissionDef[];
  selectedIds: number[];
  onChange: (newSelectedIds: number[]) => void;
}

export const MatrixUI: React.FC<MatrixUIProps> = ({ allPermissions, selectedIds, onChange }) => {
  // Nhóm các quyền theo Module
  const groupedPermissions = allPermissions.reduce(
    (acc, curr) => {
      if (!acc[curr.module]) acc[curr.module] = [];
      acc[curr.module].push(curr);
      return acc;
    },
    {} as Record<string, PermissionDef[]>
  );

  const handleTogglePermission = (id: number) => {
    if (selectedIds.includes(id)) {
      onChange(selectedIds.filter((pid) => pid !== id));
    } else {
      onChange([...selectedIds, id]);
    }
  };

  const handleToggleModule = (module: string, modulePermissions: PermissionDef[]) => {
    const moduleIds = modulePermissions.map((p) => p.id);
    const isAllSelected = moduleIds.every((id) => selectedIds.includes(id));

    if (isAllSelected) {
      // Bỏ chọn toàn bộ module
      onChange(selectedIds.filter((id) => !moduleIds.includes(id)));
    } else {
      // Chọn toàn bộ module (Dùng Set để tránh trùng lặp)
      const newSet = new Set([...selectedIds, ...moduleIds]);
      onChange(Array.from(newSet));
    }
  };

  // Helper bóc tách số thứ tự phân hệ Module (VD: "1. Hệ thống" -> 1, "10. Vận hành Kho" -> 10)
  const extractModuleOrder = (moduleName: string): number => {
    const match = moduleName.match(/^(\d+)\./);
    return match ? parseInt(match[1], 10) : 999;
  };

  const sortedModules = Object.entries(groupedPermissions).sort(
    ([modA], [modB]) => extractModuleOrder(modA) - extractModuleOrder(modB)
  );

  return (
    <div className="flex flex-col gap-6">
      {sortedModules.map(([module, permissions]) => {
        const moduleIds = permissions.map((p) => p.id);
        const isAllSelected = moduleIds.every((id) => selectedIds.includes(id));
        const isIndeterminate = !isAllSelected && moduleIds.some((id) => selectedIds.includes(id));

        return (
          <div
            key={module}
            className="bg-white border border-slate-200 rounded-xl overflow-hidden shadow-sm"
          >
            {/* Header của Module */}
            <div
              className="flex justify-between items-center px-5 py-3 bg-slate-50 border-b border-slate-200 cursor-pointer hover:bg-slate-100 transition-colors"
              onClick={() => handleToggleModule(module, permissions)}
            >
              <div className="flex items-center gap-3">
                <ShieldCheck size={20} className="text-amber-500" />
                <h3 className="font-bold text-slate-700 uppercase tracking-wide text-[13px]">
                  {module}
                </h3>
              </div>
              <button
                type="button"
                className="flex items-center gap-2 text-[12px] font-bold text-slate-500"
              >
                {isAllSelected ? (
                  <>
                    <CheckSquare size={18} className="text-emerald-500" /> Đã chọn tất cả
                  </>
                ) : isIndeterminate ? (
                  <>
                    <CheckSquare size={18} className="text-amber-500 opacity-60" /> Đã chọn một phần
                  </>
                ) : (
                  <>
                    <Square size={18} /> Chọn tất cả
                  </>
                )}
              </button>
            </div>

            {/* Danh sách quyền bên trong */}
            <div className="p-5 grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4 bg-slate-50/30">
              {permissions.map((perm) => {
                const isChecked = selectedIds.includes(perm.id);
                return (
                  <label
                    key={perm.id}
                    className={`flex items-start gap-3 p-3 rounded-lg border-2 cursor-pointer transition-all duration-200 ${
                      isChecked
                        ? 'border-emerald-500 bg-emerald-50/50 shadow-[0_2px_8px_rgba(16,185,129,0.15)]'
                        : 'border-slate-200 bg-white hover:border-amber-300 hover:bg-amber-50/30'
                    }`}
                  >
                    <div className="mt-0.5 shrink-0 transition-transform duration-200 hover:scale-110">
                      {isChecked ? (
                        <CheckSquare size={20} className="text-emerald-600" />
                      ) : (
                        <Square size={20} className="text-slate-300" />
                      )}
                    </div>
                    <div className="flex flex-col">
                      <span
                        className={`text-[13.5px] font-bold leading-tight ${isChecked ? 'text-emerald-800' : 'text-slate-700'}`}
                      >
                        {perm.name}
                      </span>
                      <span className="text-[11px] font-medium text-slate-400 mt-1 uppercase tracking-wider">
                        {perm.code}
                      </span>
                    </div>
                    {/* Input ẩn để quản lý Form Event */}
                    <input
                      type="checkbox"
                      className="hidden"
                      checked={isChecked}
                      onChange={() => handleTogglePermission(perm.id)}
                    />
                  </label>
                );
              })}
            </div>
          </div>
        );
      })}
    </div>
  );
};

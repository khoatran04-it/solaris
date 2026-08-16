import axiosClient from './axiosClient';
import { PermissionDef } from '.././components/commons/MatrixUI';

export const permissionApi = {
    // Chỉ cần 1 hàm duy nhất để lấy Master Data
    getAll: (): Promise<PermissionDef[]> => {
        return axiosClient.get('/ia-permissions');
    }
};
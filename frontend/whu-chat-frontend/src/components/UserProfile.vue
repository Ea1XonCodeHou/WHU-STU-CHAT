<template>
  <div class="modal-backdrop" @click.self="closeModal">
    <div class="modal-content">
      <div class="modal-header">
        <h3>个人资料</h3>
        <button class="close-button" @click="closeModal">
          <i class="fas fa-times"></i>
        </button>
      </div>
      
      <div class="modal-body">
        <div class="profile-section">
          <div class="avatar-section">
            <div class="avatar-container">
              <img v-if="avatarUrl" :src="avatarUrl" alt="用户头像" class="profile-avatar">
              <div v-else class="avatar-placeholder">{{ userInitials }}</div>
              
              <div class="avatar-upload-overlay" @click="triggerFileUpload">
                <i class="fas fa-camera"></i>
                <span>更换头像</span>
              </div>
              <input 
                type="file" 
                ref="fileInput" 
                style="display: none" 
                accept="image/*" 
                @change="handleFileChange"
              >
            </div>
            <div v-if="avatarLoading" class="upload-loading">
              <i class="fas fa-spinner fa-spin"></i> 上传中...
            </div>
          </div>
          
          <form class="profile-form" @submit.prevent="saveProfile">
            <div class="form-group">
              <label for="username">用户名</label>
              <input 
                type="text" 
                id="username" 
                v-model="userProfile.username" 
                placeholder="请输入用户名"
              >
            </div>
            
            <div class="form-group">
              <label for="email">邮箱</label>
              <input 
                type="email" 
                id="email" 
                v-model="userProfile.email" 
                placeholder="请输入邮箱"
              >
            </div>
            
            <div class="form-group">
              <label for="phone">手机号</label>
              <input 
                type="tel" 
                id="phone" 
                v-model="userProfile.phone" 
                placeholder="请输入手机号"
              >
            </div>
            
            <div class="form-group">
              <label for="signature">个人签名</label>
              <textarea 
                id="signature" 
                v-model="userProfile.signature" 
                placeholder="请输入个人签名"
                rows="3"
              ></textarea>
            </div>
            
            <div class="form-actions">
              <button 
                type="submit" 
                class="save-button" 
                :disabled="isSaving"
              >
                <i v-if="isSaving" class="fas fa-spinner fa-spin"></i>
                <span v-else>保存修改</span>
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import { ref, computed, onMounted } from 'vue';
import axios from 'axios';

export default {
  name: 'UserProfile',
  props: {
    visible: {
      type: Boolean,
      required: true
    }
  },
  emits: ['close', 'profile-updated'],
  setup(props, { emit }) {
    // 用户信息
    const userId = ref(localStorage.getItem('userId') || '');
    const userProfile = ref({
      username: localStorage.getItem('username') || '',
      email: localStorage.getItem('userEmail') || '',
      phone: localStorage.getItem('userPhone') || '',
      signature: localStorage.getItem('userSignature') || ''
    });
    
    // 头像相关
    const avatarUrl = ref(localStorage.getItem('userAvatar') || '');
    const fileInput = ref(null);
    const avatarLoading = ref(false);
    
    // 保存按钮状态
    const isSaving = ref(false);
    
    // 计算用户名首字母作为头像占位符
    const userInitials = computed(() => {
      return userProfile.value.username ? userProfile.value.username.charAt(0).toUpperCase() : '?';
    });
    
    // 加载用户信息
    const loadUserProfile = async () => {
      try {
        const response = await axios.get(`/api/user/${userId.value}`);
        console.log('获取用户信息响应:', response);
        
        // 直接从response.data中获取数据，不假设嵌套的data属性
        const userData = response.data;
        userProfile.value = {
          username: userData.username || '',
          email: userData.email || '',
          phone: userData.phone || '',
          signature: userData.signature || ''
        };
        
        // 更新头像URL
        if (userData.avatar) {
          avatarUrl.value = userData.avatar;
          localStorage.setItem('userAvatar', userData.avatar);
        }
        
        // 更新localStorage
        localStorage.setItem('userEmail', userData.email || '');
        localStorage.setItem('userPhone', userData.phone || '');
        localStorage.setItem('userSignature', userData.signature || '');
      } catch (error) {
        console.error('获取用户信息失败:', error);
      }
    };
    
    // 保存个人资料
    const saveProfile = async () => {
      isSaving.value = true;
      try {
        // 准备要发送的用户数据，确保处理null值
        const userData = {
          userId: parseInt(userId.value),
          username: userProfile.value.username.trim(),
          // 处理可能为空的字段，传递null而不是空字符串
          email: userProfile.value.email?.trim() || null,
          phone: userProfile.value.phone?.trim() || null,
          signature: userProfile.value.signature?.trim() || null,
          // 添加password字段并设为null
          password: null
        };
        
        console.log('正在更新用户信息:', userData);
        
        try {
          // 确保使用正确的userId，并以JSON格式发送请求
          const response = await axios({
            method: 'put',
            url: `/api/user/${userId.value}`,
            data: userData,
            headers: {
              'Content-Type': 'application/json'
            },
            timeout: 10000 // 设置10秒超时
          });
          
          console.log('更新用户信息响应:', response);
          
          if (response.data) {
            // 更新localStorage
            localStorage.setItem('username', userProfile.value.username);
            localStorage.setItem('userEmail', userProfile.value.email || '');
            localStorage.setItem('userPhone', userProfile.value.phone || '');
            localStorage.setItem('userSignature', userProfile.value.signature || '');
            
            // 通知父组件更新成功
            emit('profile-updated', {
              username: userProfile.value.username,
              email: userProfile.value.email,
              phone: userProfile.value.phone,
              signature: userProfile.value.signature,
              avatar: avatarUrl.value
            });
            
            alert('个人资料更新成功');
          } else {
            alert('更新个人资料失败: ' + (response.data?.message || '未知错误'));
          }
        } catch (apiError) {
          console.error('API调用错误:', apiError);
          let errorMsg = '更新失败';
          
          if (apiError.response) {
            // 请求成功发出且服务器也响应了状态码，但状态代码超出了2xx的范围
            if (apiError.response.status === 400) {
              errorMsg = '请求数据格式错误，请检查填写的信息';
              console.log('后端返回的详细错误:', apiError.response.data);
            } else if (apiError.response.status === 404) {
              errorMsg = '用户不存在，请重新登录';
            } else {
              errorMsg = apiError.response.data?.message || `服务器返回错误(${apiError.response.status})`;
            }
          } else if (apiError.request) {
            // 请求已经发出，但没有收到响应
            errorMsg = '无法连接到服务器，请检查网络连接';
          } else {
            // 发送请求时出了点问题
            errorMsg = apiError.message;
          }
          
          alert('更新个人资料失败: ' + errorMsg);
        }
      } catch (error) {
        console.error('一般错误:', error);
        alert('更新个人资料出现未知错误: ' + error.message);
      } finally {
        isSaving.value = false;
      }
    };
    
    // 触发文件上传
    const triggerFileUpload = () => {
      fileInput.value.click();
    };
    
    // 处理文件上传
    const handleFileChange = async (e) => {
      const file = e.target.files[0];
      if (!file) return;
      
      // 验证文件类型
      const validTypes = ['image/jpeg', 'image/png', 'image/gif', 'image/jpg'];
      if (!validTypes.includes(file.type)) {
        alert('请上传JPG、JPEG、PNG或GIF格式的图片');
        return;
      }
      
      // 验证文件大小（最大5MB）
      if (file.size > 5 * 1024 * 1024) {
        alert('图片大小不能超过5MB');
        return;
      }
      
      avatarLoading.value = true;
      
      try {
        console.log('准备上传头像:', file);
        
        // 创建FormData对象上传文件
        const formData = new FormData();
        formData.append('file', file);
        
        const apiBaseUrl = window.apiBaseUrl || '';
        const uploadUrl = `${apiBaseUrl}/api/user/${userId.value}/avatar`;
        console.log('请求URL:', uploadUrl);
        
        const response = await axios.post(uploadUrl, formData, {
          headers: {
            'Content-Type': 'multipart/form-data'
          }
        });
        
        console.log('上传头像响应:', response);
        
        // 检查响应数据
        if (response.data) {
          // 确保获取到完整的URL路径
          let avatarPath = response.data;
          console.log('原始头像路径:', avatarPath);
          
          // 如果是相对路径，添加基础URL
          if (avatarPath && avatarPath.startsWith('/')) {
            const origin = window.location.origin;
            avatarPath = `${origin}${avatarPath}`;
          }
          
          console.log('最终头像路径:', avatarPath);
          
          // 确保图片缓存更新
          avatarPath = avatarPath + '?t=' + new Date().getTime();
          
          // 更新头像URL
          avatarUrl.value = avatarPath;
          localStorage.setItem('userAvatar', avatarPath);
          
          // 通知父组件
          emit('profile-updated', {
            ...userProfile.value,
            avatar: avatarPath
          });
          
          alert('头像上传成功');
        } else {
          alert('上传头像失败: ' + (response.data?.message || '未知错误'));
        }
      } catch (error) {
        console.error('上传头像失败:', error);
        alert('上传头像失败: ' + (error.response?.data?.message || error.message));
      } finally {
        avatarLoading.value = false;
        // 重置文件输入
        fileInput.value.value = '';
      }
    };
    
    // 关闭模态框
    const closeModal = () => {
      emit('close');
    };
    
    // 组件挂载时加载用户信息
    onMounted(() => {
      if (userId.value) {
        loadUserProfile();
      }
    });
    
    return {
      userId,
      userProfile,
      avatarUrl,
      fileInput,
      isSaving,
      avatarLoading,
      userInitials,
      saveProfile,
      closeModal,
      triggerFileUpload,
      handleFileChange
    };
  }
};
</script>

<style scoped>
.modal-backdrop {
  position: fixed;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  background-color: rgba(0, 0, 0, 0.5);
  display: flex;
  justify-content: center;
  align-items: center;
  z-index: 1000;
}

.modal-content {
  width: 500px;
  max-width: 90%;
  background-color: white;
  border-radius: 8px;
  box-shadow: 0 4px 20px rgba(0, 0, 0, 0.15);
  overflow: hidden;
}

.modal-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 16px 20px;
  background: linear-gradient(135deg, #4776E6 0%, #8E54E9 100%);
  color: white;
}

.modal-header h3 {
  margin: 0;
  font-size: 18px;
  font-weight: 600;
}

.close-button {
  background: transparent;
  border: none;
  color: white;
  font-size: 16px;
  cursor: pointer;
  padding: 5px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 50%;
  width: 30px;
  height: 30px;
  transition: background-color 0.2s;
}

.close-button:hover {
  background-color: rgba(255, 255, 255, 0.2);
}

.modal-body {
  padding: 20px;
}

.profile-section {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.avatar-section {
  display: flex;
  flex-direction: column;
  align-items: center;
  margin-bottom: 20px;
}

.avatar-container {
  position: relative;
  width: 100px;
  height: 100px;
  border-radius: 50%;
  overflow: hidden;
  box-shadow: 0 4px 10px rgba(0, 0, 0, 0.1);
}

.profile-avatar {
  width: 100%;
  height: 100%;
  object-fit: cover;
  border-radius: 50%;
}

.avatar-placeholder {
  width: 100%;
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, #4776E6 0%, #8E54E9 100%);
  color: white;
  font-size: 36px;
  font-weight: bold;
}

.avatar-upload-overlay {
  position: absolute;
  bottom: 0;
  left: 0;
  right: 0;
  background-color: rgba(0, 0, 0, 0.6);
  color: white;
  padding: 8px 0;
  text-align: center;
  font-size: 12px;
  cursor: pointer;
  opacity: 0;
  transition: opacity 0.2s;
}

.avatar-container:hover .avatar-upload-overlay {
  opacity: 1;
}

.upload-loading {
  margin-top: 10px;
  color: #4776E6;
  font-size: 14px;
}

.profile-form {
  display: flex;
  flex-direction: column;
  gap: 15px;
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: 5px;
}

.form-group label {
  font-size: 14px;
  font-weight: 500;
  color: #555;
}

.form-group input,
.form-group textarea {
  padding: 10px 12px;
  border: 1px solid #ddd;
  border-radius: 4px;
  font-size: 14px;
  transition: border-color 0.2s;
}

.form-group input:focus,
.form-group textarea:focus {
  border-color: #4776E6;
  outline: none;
}

.form-actions {
  display: flex;
  justify-content: flex-end;
  margin-top: 10px;
}

.save-button {
  background: linear-gradient(135deg, #4776E6 0%, #8E54E9 100%);
  color: white;
  border: none;
  padding: 10px 20px;
  border-radius: 4px;
  cursor: pointer;
  font-weight: 500;
  display: flex;
  align-items: center;
  justify-content: center;
  min-width: 100px;
  transition: transform 0.2s, box-shadow 0.2s;
}

.save-button:hover:not(:disabled) {
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(71, 118, 230, 0.3);
}

.save-button:disabled {
  opacity: 0.7;
  cursor: not-allowed;
}
</style> 
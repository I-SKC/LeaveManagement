import { ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService, LeaveRequest, LeaveType } from '../../services/auth.service';
@Component({selector:'app-leave-details',standalone:true,imports:[CommonModule,RouterLink],templateUrl:'./leave-details.html',styleUrl:'./leave-details.css'})
export class LeaveDetails implements OnInit {
 private api=inject(AuthService);private route=inject(ActivatedRoute);private router=inject(Router);private cdr=inject(ChangeDetectorRef);
 username=this.api.getUsername();request:LeaveRequest|null=null;types:LeaveType[]=[];loading=true;error='';cancelNotice='';
 ngOnInit(){this.api.getLeaveTypes().subscribe({next:x=>{this.types=x;this.cdr.markForCheck();},error:()=>{}});this.load();}
 load(){const id=this.api.getEmployeeId();const requestId=Number(this.route.snapshot.paramMap.get('id'));if(!id||!requestId){this.error='Invalid employee or request ID.';this.loading=false;this.cdr.markForCheck();return;}this.loading=true;this.api.getMyLeaveRequests(id).subscribe({next:items=>{this.request=items.find(x=>x.leaveRequestId===requestId)||null;if(!this.request)this.error='This request was not found in your leave history.';this.loading=false;this.cdr.markForCheck();},error:e=>{this.error=e.error?.message||`Unable to load request (${e.status}).`;this.loading=false;this.cdr.markForCheck();}});}
 canCancel(item:LeaveRequest|null){if(!item)return false;const today=new Date().toISOString().split('T')[0];if(item.status==='Pending')return true;return item.status==='Approved'&&item.startDate>today;}
 cancelRequest(){const item=this.request;if(!item)return;if(!window.confirm(`Cancel leave request #${item.leaveRequestId} (${item.startDate} – ${item.endDate})? This cannot be undone.`))return;this.error='';this.cancelNotice='';this.api.cancelLeaveRequest(item.leaveRequestId).subscribe({next:()=>{this.cancelNotice=`Leave request #${item.leaveRequestId} was cancelled.`;this.load();},error:e=>{this.error=e.error?.message||`Unable to cancel request (${e.status}).`;this.cdr.markForCheck();}});}
 leaveType(id:number){return this.types.find(t=>t.leaveTypeId===id)?.name||`Leave Type ${id}`;}
}
